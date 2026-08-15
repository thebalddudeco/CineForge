from __future__ import annotations

import json
import shutil
import subprocess
from pathlib import Path
from typing import Any

from .config import Settings
from .wan22 import PACK_ID as WAN22_PACK_ID, find_pack as find_wan22_pack


MODEL_EXTENSIONS = {".safetensors", ".ckpt", ".pt", ".pth", ".bin", ".gguf"}

FAMILY_RULES = [
    ("wan2.2", "Wan 2.2", "video", ("wan2.2", "wan22")),
    ("ltx23", "LTX 2.3", "video", ("ltx23", "ltxv23", "ltx-2.3")),
    ("flux2", "Flux 2", "image", ("flux2", "flux_2", "flux-2")),
    ("qwen-image", "Qwen Image", "image", ("qwen_image", "qwen-image")),
    ("redcraft", "RedCraft", "image", ("redcraft",)),
    ("moody-real", "Moody Real Mix", "image", ("moodyrealmix", "moody_real")),
    ("anima", "Anima", "image", ("anima",)),
    ("mistral", "Mistral", "text-encoder", ("mistral",)),
    ("qwen-vl", "Qwen Vision-Language", "vision-encoder", ("qwen3vl", "qwen_2.5_vl", "qwen2.5-vl")),
]


def native_runtime_status(settings: Settings) -> dict[str, Any]:
    """Report the GPU directly. This probe does not contact or import ComfyUI."""
    result: dict[str, Any] = {
        "online": False,
        "backend": "native",
        "engine": "CineForge Engine",
        "engine_version": "0.5.0",
        "url": None,
    }
    command = shutil.which("nvidia-smi")
    if not command:
        result["error"] = "No supported NVIDIA runtime was detected."
        return result
    try:
        completed = subprocess.run(
            [command, "--query-gpu=name,memory.total,memory.free,utilization.gpu,temperature.gpu,power.draw,driver_version", "--format=csv,noheader,nounits"],
            check=True, capture_output=True, text=True, timeout=8,
            creationflags=getattr(subprocess, "CREATE_NO_WINDOW", 0),
        )
        line = completed.stdout.strip().splitlines()[0]
        name, total_mb, free_mb, utilization, temperature, power, driver = [part.strip() for part in line.split(",", 6)]
        def number(value: str, fallback: float = 0.0) -> float:
            try:
                return float(value)
            except ValueError:
                return fallback
        result.update({
            "online": True,
            "device": name,
            "vram_total_gb": round(float(total_mb) / 1024, 1),
            "vram_free_gb": round(float(free_mb) / 1024, 1),
            "gpu_utilization_percent": number(utilization),
            "temperature_c": number(temperature),
            "power_w": number(power),
            "driver_version": driver,
            "python_version": None,
        })
    except (OSError, ValueError, subprocess.SubprocessError, IndexError) as exc:
        result["error"] = str(exc)
    return result


def _family_for(name: str) -> tuple[str, str, str] | None:
    lowered = name.lower()
    for family_id, label, capability, tokens in FAMILY_RULES:
        if any(token in lowered for token in tokens):
            return family_id, label, capability
    return None


def discover_models(settings: Settings) -> dict[str, Any]:
    assets: list[dict[str, Any]] = []
    families: dict[str, dict[str, Any]] = {}
    visited: set[str] = set()
    native_packs: list[dict[str, Any]] = []
    for root_text in settings.model_roots:
        root = Path(root_text)
        if not root.exists():
            continue
        wan_pack = find_wan22_pack(root)
        if wan_pack is not None and not any(item["id"] == WAN22_PACK_ID for item in native_packs):
            native_packs.append({
                "id": WAN22_PACK_ID,
                "label": "CineForge Wan 2.2 I2V A14B FP8",
                "kind": "video",
                "capability": "video",
                "pipeline": "WanImageToVideoPipeline",
                "path": str(wan_pack.resolve()),
                "available": True,
                "native_pack": True,
                "native_format": "cineforge-wan22-scaled-fp8",
                "reference": False,
                "status": "native scaled-FP8 model pack",
                "diagnostic": False,
            })
        for index_path in root.rglob("model_index.json"):
            try:
                metadata = json.loads(index_path.read_text(encoding="utf-8"))
            except (OSError, json.JSONDecodeError):
                continue
            pipeline = str(metadata.get("_class_name") or "DiffusionPipeline")
            pack_manifest: dict[str, Any] = {}
            manifest_path = index_path.parent / "cineforge-model.json"
            if manifest_path.is_file():
                try:
                    pack_manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
                except (OSError, json.JSONDecodeError):
                    pack_manifest = {}
            lowered = pipeline.lower()
            capability = "video" if any(token in lowered for token in ("video", "wan", "ltx")) else "image"
            pack_id = str(pack_manifest.get("id") or ("native-" + index_path.parent.name.lower().replace("_", "-").replace(" ", "-")))
            native_packs.append({
                "id": pack_id, "label": str(pack_manifest.get("label") or index_path.parent.name), "kind": "video" if capability == "video" else "still",
                "capability": capability, "pipeline": pipeline, "path": str(index_path.parent.resolve()),
                "available": True, "native_pack": True, "reference": capability == "image",
                "status": "standalone model pack", "diagnostic": bool(pack_manifest.get("diagnostic")),
            })
        for path in root.rglob("*"):
            if not path.is_file() or path.suffix.lower() not in MODEL_EXTENSIONS:
                continue
            key = str(path.resolve()).lower()
            if key in visited:
                continue
            visited.add(key)
            family = _family_for(path.name)
            if not family:
                continue
            family_id, label, capability = family
            item = {
                "name": path.name,
                "path": str(path),
                "size_gb": round(path.stat().st_size / 1024**3, 2),
                "family_id": family_id,
                "capability": capability,
            }
            assets.append(item)
            bucket = families.setdefault(family_id, {
                "id": family_id, "label": label, "capability": capability,
                "asset_count": 0, "size_gb": 0.0, "runnable": False,
            })
            bucket["asset_count"] += 1
            bucket["size_gb"] = round(bucket["size_gb"] + item["size_gb"], 2)
    capabilities = {"native_engine": True}
    for family in families.values():
        family["runnable"] = False
        family["status"] = "raw assets detected; standalone model-pack conversion required"
    adapters = [
        {"id": "anima-aesthetic", "label": "Anima Aesthetic", "kind": "still", "available": False, "reference": True, "status": "conversion required"},
        {"id": "moody-real", "label": "Moody Real Mix", "kind": "still", "available": False, "reference": True, "status": "conversion required"},
        {"id": "redcraft", "label": "RedCraft", "kind": "still", "available": False, "reference": True, "status": "conversion required"},
        {"id": "ltx23", "label": "LTX 2.3 GTAnimation", "kind": "video", "available": False, "reference": True, "status": "conversion required"},
        {"id": "wan2.2", "label": "Wan 2.2", "kind": "video", "available": False, "reference": True, "status": "conversion required"},
    ]
    adapters = native_packs + adapters
    return {
        "roots": settings.model_roots,
        "families": sorted(families.values(), key=lambda item: (item["capability"], item["label"])),
        "assets": sorted(assets, key=lambda item: item["size_gb"], reverse=True),
        "capabilities": capabilities,
        "adapters": adapters,
        "backend": "native",
    }
