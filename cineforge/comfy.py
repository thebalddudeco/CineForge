from __future__ import annotations

import json
import mimetypes
import random
import urllib.parse
import urllib.request
import uuid
from pathlib import Path
from typing import Any

from .config import RESOURCE_ROOT, Settings


IMAGE_MODELS = {
    "anima-aesthetic": {
        "unet": "anima-aesthetic-v1.1.safetensors", "clip": "qwen_3_06b_base.safetensors",
        "clip_type": "stable_diffusion", "vae": "qwen_image_vae.safetensors", "cfg": 4.5,
    },
    "moody-real": {
        "unet": "moodyRealMix_xhsEdition.safetensors", "clip": "qwen_2.5_vl_7b_fp8_scaled.safetensors",
        "clip_type": "qwen_image", "vae": "qwen_image_vae.safetensors", "cfg": 3.5,
    },
    "redcraft": {
        "unet": "redcraft23INT8INT4FP8_30Krea2.safetensors", "clip": "qwen3vl_4b_fp8_scaled.safetensors",
        "clip_type": "krea2", "vae": "qwen_image_vae.safetensors", "cfg": 3.5,
    },
}


class ComfyClient:
    def __init__(self, settings: Settings):
        self.settings = settings

    def _json(self, path: str, payload: dict[str, Any] | None = None) -> dict[str, Any]:
        data = json.dumps(payload).encode("utf-8") if payload is not None else None
        request = urllib.request.Request(
            f"{self.settings.comfy_url}{path}", data=data,
            headers={"Content-Type": "application/json"} if data else {},
            method="POST" if data else "GET",
        )
        with urllib.request.urlopen(request, timeout=30) as response:
            return json.loads(response.read().decode("utf-8"))

    def queue_still(self, prompt: str, negative_prompt: str = "", model_id: str = "moody-real", width: int = 768, height: int = 432, seed: int | None = None, reference_image: str | None = None, quality: str = "proof") -> dict[str, Any]:
        if model_id not in IMAGE_MODELS:
            raise ValueError(f"Unsupported still adapter: {model_id}")
        model = IMAGE_MODELS[model_id]
        seed = seed if seed is not None else random.randint(0, 2**31 - 1)
        steps = 8 if quality == "proof" else 25
        sampler = "euler" if quality == "proof" or model_id != "anima-aesthetic" else "er_sde"
        width = max(256, min(1536, round(width / 32) * 32))
        height = max(256, min(1536, round(height / 32) * 32))
        workflow: dict[str, Any] = {
            "1": {"class_type": "UNETLoader", "inputs": {"unet_name": model["unet"], "weight_dtype": "default"}},
            "2": {"class_type": "CLIPLoader", "inputs": {"clip_name": model["clip"], "type": model["clip_type"], "device": "default"}},
            "3": {"class_type": "CLIPTextEncode", "inputs": {"text": prompt, "clip": ["2", 0]}},
            "4": {"class_type": "CLIPTextEncode", "inputs": {"text": negative_prompt or "low quality, blurry, distorted face, malformed hands, duplicate people, text, watermark", "clip": ["2", 0]}},
            "5": {"class_type": "EmptyLatentImage", "inputs": {"width": width, "height": height, "batch_size": 1}},
            "6": {"class_type": "KSampler", "inputs": {"model": ["1", 0], "seed": seed, "steps": steps, "cfg": model["cfg"], "sampler_name": sampler, "scheduler": "simple", "positive": ["3", 0], "negative": ["4", 0], "latent_image": ["5", 0], "denoise": 1}},
            "7": {"class_type": "VAELoader", "inputs": {"vae_name": model["vae"]}},
            "8": {"class_type": "VAEDecode", "inputs": {"samples": ["6", 0], "vae": ["7", 0]}},
            "9": {"class_type": "SaveImage", "inputs": {"images": ["8", 0], "filename_prefix": f"cineforge/stills/{model_id}"}},
        }
        if reference_image:
            workflow["10"] = {"class_type": "LoadImage", "inputs": {"image": reference_image}}
            workflow["11"] = {"class_type": "VAEEncode", "inputs": {"pixels": ["10", 0], "vae": ["7", 0]}}
            workflow["6"]["inputs"]["latent_image"] = ["11", 0]
            workflow["6"]["inputs"]["denoise"] = 0.48
        client_id = f"cineforge-{uuid.uuid4().hex}"
        queued = self._json("/prompt", {"prompt": workflow, "client_id": client_id})
        return {"prompt_id": queued.get("prompt_id"), "number": queued.get("number"), "seed": seed, "model_id": model_id, "client_id": client_id, "steps": steps}

    def queue_video(self, image_name: str, prompt: str, negative_prompt: str = "", width: int = 768, height: int = 432, length: int = 17, seed: int | None = None, quality: str = "proof", model_id: str = "wan2.2") -> dict[str, Any]:
        seed = seed if seed is not None else random.randint(0, 2**31 - 1)
        width = max(256, min(1024, round(width / 32) * 32))
        height = max(256, min(1024, round(height / 32) * 32))
        if model_id == "wan2.2":
            length = max(17, min(241, length))
            workflow = wan22_workflow(image_name, prompt, negative_prompt, width, height, length, seed, quality == "proof")
        elif model_id == "ltx23":
            length = max(9, min(241, 1 + round((length - 1) / 8) * 8))
            steps = 12 if quality == "proof" else 20
            workflow = ltx_workflow(image_name, prompt, negative_prompt, width, height, length, seed, steps)
        else:
            raise ValueError(f"Unsupported video adapter: {model_id}")
        client_id = f"cineforge-{uuid.uuid4().hex}"
        queued = self._json("/prompt", {"prompt": workflow, "client_id": client_id})
        return {"prompt_id": queued.get("prompt_id"), "number": queued.get("number"), "seed": seed, "model_id": model_id, "client_id": client_id, "steps": None}

    def history(self, prompt_id: str) -> dict[str, Any]:
        raw = self._json(f"/history/{urllib.parse.quote(prompt_id)}")
        job = raw.get(prompt_id)
        if not job:
            queue = self._json("/queue")
            for item in queue.get("queue_running", []):
                if len(item) > 1 and str(item[1]) == prompt_id:
                    return {"prompt_id": prompt_id, "status": "running", "queue_position": 0, "outputs": []}
            for index, item in enumerate(queue.get("queue_pending", [])):
                if len(item) > 1 and str(item[1]) == prompt_id:
                    return {"prompt_id": prompt_id, "status": "queued", "queue_position": index + 1, "outputs": []}
            return {"prompt_id": prompt_id, "status": "unknown", "outputs": []}
        outputs: list[dict[str, Any]] = []
        for node in job.get("outputs", {}).values():
            for kind in ("images", "videos", "gifs", "audio"):
                for item in node.get(kind, []):
                    media = dict(item)
                    extension = Path(str(media.get("filename") or "")).suffix.lower()
                    media["kind"] = "video" if kind in {"videos", "gifs"} or extension in {".mp4", ".webm", ".mov", ".gif"} else "audio" if kind == "audio" or extension in {".wav", ".mp3", ".flac"} else "image"
                    media["url"] = "/api/media?" + urllib.parse.urlencode({
                        "filename": media.get("filename", ""),
                        "subfolder": media.get("subfolder", ""),
                        "type": media.get("type", "output"),
                    })
                    outputs.append(media)
        raw_status = job.get("status", {})
        status = "error" if raw_status.get("status_str") == "error" else "complete"
        error_message = ""
        for message in raw_status.get("messages", []):
            if isinstance(message, list) and len(message) > 1 and message[0] == "execution_error" and isinstance(message[1], dict):
                error_message = str(message[1].get("exception_message") or message[1].get("exception_type") or "Local workflow failed")
                break
        return {"prompt_id": prompt_id, "status": status, "outputs": outputs, "error": error_message, "raw_status": raw_status}

    def upload_image(self, path: Path) -> dict[str, Any]:
        boundary = f"----CineForge{uuid.uuid4().hex}"
        content_type = mimetypes.guess_type(path.name)[0] or "application/octet-stream"
        chunks = [
            f"--{boundary}\r\n".encode(),
            f'Content-Disposition: form-data; name="image"; filename="{path.name}"\r\n'.encode(),
            f"Content-Type: {content_type}\r\n\r\n".encode(), path.read_bytes(), b"\r\n",
            f"--{boundary}\r\nContent-Disposition: form-data; name=\"overwrite\"\r\n\r\ntrue\r\n".encode(),
            f"--{boundary}--\r\n".encode(),
        ]
        request = urllib.request.Request(f"{self.settings.comfy_url}/upload/image", data=b"".join(chunks), headers={"Content-Type": f"multipart/form-data; boundary={boundary}"}, method="POST")
        with urllib.request.urlopen(request, timeout=60) as response:
            return json.loads(response.read().decode("utf-8"))

    def promote_output_to_input(self, media: dict[str, Any], staging_root: Path) -> str:
        """Copy a generated ComfyUI output into its input library for image-to-video."""
        filename = Path(str(media.get("filename") or "generated-frame.png")).name
        params = urllib.parse.urlencode({
            "filename": filename,
            "subfolder": str(media.get("subfolder") or ""),
            "type": str(media.get("type") or "output"),
        })
        with urllib.request.urlopen(f"{self.settings.comfy_url}/view?{params}", timeout=60) as response:
            payload = response.read()
        staged = staging_root / f"motion-{uuid.uuid4().hex[:8]}-{filename}"
        staged.write_bytes(payload)
        uploaded = self.upload_image(staged)
        return str(uploaded.get("name") or staged.name)


def ltx_workflow(image_name: str, prompt: str, negative: str, width: int, height: int, length: int, seed: int, steps: int) -> dict[str, Any]:
    checkpoint = "ltx23Gtanimation25Frames_ltxv23INT4Convrot.safetensors"
    workflow: dict[str, Any] = {
        "1": {"class_type": "LoadImage", "inputs": {"image": image_name}},
        "2": {"class_type": "CheckpointLoaderSimple", "inputs": {"ckpt_name": checkpoint}},
        "3": {"class_type": "LTXAVTextEncoderLoader", "inputs": {"text_encoder": "mistral_3_small_flux2_bf16.safetensors", "ckpt_name": checkpoint, "device": "default"}},
        "4": {"class_type": "CLIPTextEncode", "inputs": {"clip": ["3", 0], "text": prompt}},
        "5": {"class_type": "CLIPTextEncode", "inputs": {"clip": ["3", 0], "text": negative or "identity drift, morphing, malformed hands, sliding feet, floating objects, text, watermark"}},
        "6": {"class_type": "LTXVConditioning", "inputs": {"positive": ["4", 0], "negative": ["5", 0], "frame_rate": 25}},
        "7": {"class_type": "EmptyLTXVLatentVideo", "inputs": {"width": width, "height": height, "length": length, "batch_size": 1}},
        "8": {"class_type": "LTXVImgToVideoInplace", "inputs": {"vae": ["2", 2], "image": ["1", 0], "latent": ["7", 0], "strength": 1, "bypass": False}},
        "9": {"class_type": "LTXVAddGuide", "inputs": {"positive": ["6", 0], "negative": ["6", 1], "vae": ["2", 2], "latent": ["8", 0], "image": ["1", 0], "frame_idx": 0, "strength": 1}},
        "13": {"class_type": "LoraLoaderModelOnly", "inputs": {"model": ["2", 0], "lora_name": "LTX 2.3\\ltx-face-prior-f1-profile-correction-step11019.safetensors", "strength_model": 0}},
        "14": {"class_type": "ModelSamplingLTXV", "inputs": {"model": ["13", 0], "latent": ["9", 2], "max_shift": 2.05, "base_shift": 0.95}},
        "15": {"class_type": "CFGGuider", "inputs": {"model": ["14", 0], "positive": ["9", 0], "negative": ["9", 1], "cfg": 3}},
        "16": {"class_type": "KSamplerSelect", "inputs": {"sampler_name": "euler"}},
        "17": {"class_type": "LTXVScheduler", "inputs": {"latent": ["9", 2], "steps": steps, "max_shift": 2.05, "base_shift": 0.95, "stretch": True, "terminal": 0.1}},
        "18": {"class_type": "RandomNoise", "inputs": {"noise_seed": seed}},
        "19": {"class_type": "SamplerCustomAdvanced", "inputs": {"noise": ["18", 0], "guider": ["15", 0], "sampler": ["16", 0], "sigmas": ["17", 0], "latent_image": ["9", 2]}},
        "21": {"class_type": "VAEDecode", "inputs": {"samples": ["19", 0], "vae": ["2", 2]}},
        "23": {"class_type": "CreateVideo", "inputs": {"images": ["21", 0], "fps": 25}},
        "24": {"class_type": "SaveVideo", "inputs": {"video": ["23", 0], "filename_prefix": "cineforge/video/ltx23", "format": "mp4", "codec": "h264"}},
    }
    return workflow


def wan22_workflow(image_name: str, prompt: str, negative: str, width: int, height: int, length: int, seed: int, fast_mode: bool) -> dict[str, Any]:
    """Build the installed Wan 2.2 I2V graph from the local verified template."""
    template_path = RESOURCE_ROOT / "cineforge" / "workflows" / "wan22-i2v.json"
    workflow = json.loads(template_path.read_text(encoding="utf-8"))
    workflow["97"]["inputs"]["image"] = image_name
    workflow["129:93"]["inputs"]["text"] = prompt
    workflow["129:89"]["inputs"]["text"] = negative
    workflow["129:98"]["inputs"]["width"] = width
    workflow["129:98"]["inputs"]["height"] = height
    workflow["129:98"]["inputs"]["length"] = length
    workflow["129:86"]["inputs"]["noise_seed"] = seed
    workflow["129:131"]["inputs"]["value"] = fast_mode
    workflow["108"]["inputs"]["filename_prefix"] = "cineforge/video/wan22"
    return workflow
