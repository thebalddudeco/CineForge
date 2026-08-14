from __future__ import annotations

import json
import logging
import os
import random
import shutil
import subprocess
import threading
import time
import uuid
from concurrent.futures import ThreadPoolExecutor
from dataclasses import dataclass, field
from pathlib import Path
from typing import Any, Callable

from .config import GENERATED_ROOT, UPLOADS_ROOT, Settings
from .discovery import discover_models, native_runtime_status


Progress = Callable[[int, int, str], None]


@dataclass
class EngineJob:
    job_id: str
    kind: str
    model_id: str
    payload: dict[str, Any]
    status: str = "queued"
    phase: str = "QUEUED"
    value: int = 0
    maximum: int = 0
    created_at: float = field(default_factory=time.time)
    started_at: float | None = None
    finished_at: float | None = None
    outputs: list[dict[str, Any]] = field(default_factory=list)
    error: str = ""

    def public(self) -> dict[str, Any]:
        elapsed = ((self.finished_at or time.time()) - self.started_at) if self.started_at else 0
        eta = elapsed * (self.maximum - self.value) / self.value if self.value and self.maximum > self.value else 0
        return {
            "prompt_id": self.job_id,
            "status": self.status,
            "phase": self.phase,
            "value": self.value,
            "max": self.maximum,
            "elapsed_seconds": round(max(0, elapsed), 1),
            "eta_seconds": round(max(0, eta), 1),
            "outputs": list(self.outputs),
            "error": self.error,
            "backend": "native",
        }


class NativePipelineAdapter:
    """Loads standard Diffusers model-pack directories without importing ComfyUI."""

    def __init__(self, settings: Settings):
        self.settings = settings
        self._pipeline: Any = None
        self._pipeline_id: str | None = None
        self._lock = threading.Lock()

    def _pack(self, model_id: str) -> dict[str, Any]:
        catalog = discover_models(self.settings)
        for adapter in catalog["adapters"]:
            if adapter["id"] == model_id and adapter.get("native_pack"):
                return adapter
        raise ValueError(
            f"{model_id} is installed as raw workflow assets, not as a standalone CineForge model pack. "
            "Install or convert a Diffusers-format pack first."
        )

    @staticmethod
    def _configure_desktop_pipeline(pipeline: Any) -> None:
        """Keep local Desktop inference free of application-level content filtering."""
        if hasattr(pipeline, "safety_checker"):
            pipeline.safety_checker = None

    def _load(self, pack: dict[str, Any], progress: Progress) -> Any:
        with self._lock:
            if self._pipeline_id == pack["id"] and self._pipeline is not None:
                return self._pipeline
            progress(0, 5, "IMPORTING NATIVE RUNTIME")
            try:
                import torch
                from diffusers import DiffusionPipeline
                from diffusers.utils import logging as diffusers_logging
                from transformers.utils import logging as transformers_logging
                diffusers_logging.disable_progress_bar()
                transformers_logging.disable_progress_bar()
                try:
                    from huggingface_hub.utils import disable_progress_bars
                    disable_progress_bars()
                except ImportError:
                    pass
            except ImportError as exc:
                raise RuntimeError(
                    f"The CineForge GPU runtime could not load {exc.name or 'a required component'}: {exc}."
                ) from exc
            progress(1, 5, "RESOLVING MODEL PACK")
            cache = str(self.settings.model_cache_root or "")
            if cache:
                os.environ["HF_HOME"] = cache
                os.environ["HUGGINGFACE_HUB_CACHE"] = str(Path(cache) / "hub")
            progress(2, 5, "LOADING MODEL WEIGHTS")
            pipeline = DiffusionPipeline.from_pretrained(
                pack["path"], local_files_only=True, torch_dtype=torch.float16,
            )
            progress(3, 5, "CONFIGURING PIPELINE")
            self._configure_desktop_pipeline(pipeline)
            pipeline.set_progress_bar_config(disable=True)
            if torch.cuda.is_available():
                progress(4, 5, "MOVING MODEL TO GPU")
                try:
                    pipeline.enable_model_cpu_offload()
                except (ImportError, AttributeError):
                    pipeline.to("cuda")
            self._pipeline = pipeline
            self._pipeline_id = pack["id"]
            return pipeline

    @staticmethod
    def _callback(progress: Progress, steps: int):
        def report(_pipe: Any, step: int, _timestep: Any, kwargs: dict[str, Any]) -> dict[str, Any]:
            progress(step + 1, steps, "SAMPLING")
            return kwargs
        return report

    def generate(self, kind: str, model_id: str, payload: dict[str, Any], progress: Progress) -> list[Path]:
        import torch
        from PIL import Image

        pack = self._pack(model_id)
        pipeline = self._load(pack, progress)
        seed = int(payload["seed"])
        steps = int(payload["steps"])
        generator_device = "cuda" if torch.cuda.is_available() else "cpu"
        generator = torch.Generator(device=generator_device).manual_seed(seed)
        common: dict[str, Any] = {
            "prompt": payload["prompt"],
            "negative_prompt": payload.get("negative_prompt") or None,
            "num_inference_steps": steps,
            "generator": generator,
            "callback_on_step_end": self._callback(progress, steps),
        }
        if kind == "still":
            common.update(width=payload["width"], height=payload["height"])
            reference = payload.get("reference_image")
            if reference:
                common["image"] = Image.open(reference).convert("RGB")
            result = pipeline(**common)
            output = GENERATED_ROOT / "stills" / f"{model_id}-{uuid.uuid4().hex[:10]}.png"
            output.parent.mkdir(parents=True, exist_ok=True)
            result.images[0].save(output)
            return [output]
        image = Image.open(payload["image_path"]).convert("RGB")
        common.update(image=image, width=payload["width"], height=payload["height"], num_frames=payload["length"])
        result = pipeline(**common)
        output = GENERATED_ROOT / "video" / f"{model_id}-{uuid.uuid4().hex[:10]}.mp4"
        output.parent.mkdir(parents=True, exist_ok=True)
        try:
            from diffusers.utils import export_to_video
            export_to_video(result.frames[0], str(output), fps=25)
        except ImportError as exc:
            raise RuntimeError("The native video export component is not installed.") from exc
        return [output]


class NativeEngine:
    def __init__(self, settings: Settings, adapter: NativePipelineAdapter | None = None):
        self.settings = settings
        self.adapter = adapter or NativePipelineAdapter(settings)
        self.jobs: dict[str, EngineJob] = {}
        self._lock = threading.Lock()
        self._executor = ThreadPoolExecutor(max_workers=1, thread_name_prefix="cineforge-native")

    def runtime(self) -> dict[str, Any]:
        status = native_runtime_status(self.settings)
        checks: dict[str, str] = {}
        try:
            import torch
            checks["torch"] = str(torch.__version__)
        except Exception as exc:
            checks["torch"] = f"error: {exc.__class__.__name__}: {exc}"
        try:
            from diffusers import DiffusionPipeline
            checks["diffusers"] = str(__import__("diffusers").__version__)
        except Exception as exc:
            checks["diffusers"] = f"error: {exc.__class__.__name__}: {exc}"
        try:
            import transformers
            checks["transformers"] = str(transformers.__version__)
        except Exception as exc:
            checks["transformers"] = f"error: {exc.__class__.__name__}: {exc}"
        status["components"] = checks
        status["inference_ready"] = status.get("online", False) and all(not value.startswith("error:") for value in checks.values())
        return status

    def close(self) -> None:
        self._executor.shutdown(wait=False, cancel_futures=True)

    def models(self) -> dict[str, Any]:
        return discover_models(self.settings)

    def upload_image(self, path: Path) -> dict[str, Any]:
        return {"name": path.name, "path": str(path.resolve()), "type": "input"}

    def queue_still(self, **payload: Any) -> dict[str, Any]:
        payload["seed"] = int(payload.get("seed") or random.randint(0, 2**31 - 1))
        payload["steps"] = 8 if payload.get("quality") == "proof" else 25
        reference = payload.get("reference_image")
        if reference and not Path(str(reference)).is_absolute():
            payload["reference_image"] = str((UPLOADS_ROOT / Path(str(reference)).name).resolve())
        return self._queue("still", str(payload.get("model_id") or ""), payload)

    def queue_video(self, **payload: Any) -> dict[str, Any]:
        payload["seed"] = int(payload.get("seed") or random.randint(0, 2**31 - 1))
        payload["steps"] = 8 if payload.get("quality") == "proof" else 20
        return self._queue("video", str(payload.get("model_id") or ""), payload)

    def _queue(self, kind: str, model_id: str, payload: dict[str, Any]) -> dict[str, Any]:
        if not model_id:
            raise ValueError("No native model adapter is available for this generation type.")
        job = EngineJob(uuid.uuid4().hex, kind, model_id, payload, maximum=int(payload["steps"]))
        with self._lock:
            self.jobs[job.job_id] = job
        self._executor.submit(self._run, job)
        return {
            "prompt_id": job.job_id, "seed": payload["seed"], "model_id": model_id,
            "client_id": None, "steps": job.maximum, "backend": "native",
        }

    def _run(self, job: EngineJob) -> None:
        job.status = "running"
        job.phase = "PREPARING NATIVE PIPELINE"
        job.started_at = time.time()

        def progress(value: int, maximum: int, phase: str) -> None:
            job.value, job.maximum, job.phase = int(value), int(maximum), phase

        try:
            paths = self.adapter.generate(job.kind, job.model_id, job.payload, progress)
            job.outputs = [self._media(path) for path in paths]
            job.value = job.maximum or 1
            job.maximum = job.maximum or 1
            job.phase = "OUTPUT SAVED"
            job.status = "complete"
        except Exception as exc:
            logging.exception("Native %s job %s failed during %s", job.kind, job.job_id, job.phase)
            job.error = str(exc)
            job.phase = "NATIVE ENGINE ERROR"
            job.status = "error"
        finally:
            job.finished_at = time.time()

    def history(self, job_id: str) -> dict[str, Any]:
        with self._lock:
            job = self.jobs.get(job_id)
        return job.public() if job else {"prompt_id": job_id, "status": "unknown", "outputs": [], "backend": "native"}

    @staticmethod
    def _media(path: Path) -> dict[str, Any]:
        relative = path.resolve().relative_to(GENERATED_ROOT.resolve())
        extension = path.suffix.lower()
        kind = "video" if extension in {".mp4", ".webm", ".mov", ".gif"} else "image"
        return {
            "filename": path.name, "subfolder": relative.parent.as_posix(), "type": "output",
            "kind": kind, "url": "/api/media?path=" + relative.as_posix(), "path": str(path.resolve()),
        }

    def media_path(self, value: str) -> Path:
        root = GENERATED_ROOT.resolve()
        candidate = (root / value).resolve()
        if candidate != root and root not in candidate.parents:
            raise ValueError("Invalid media path")
        if not candidate.is_file():
            raise FileNotFoundError("Generated media not found")
        return candidate

    def prepare_video_input(self, media: dict[str, Any]) -> str:
        direct = Path(str(media.get("path") or ""))
        if direct.is_file():
            return str(direct.resolve())
        relative = "/".join(filter(None, [str(media.get("subfolder") or ""), str(media.get("filename") or "")]))
        return str(self.media_path(relative))
