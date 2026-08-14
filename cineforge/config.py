from __future__ import annotations

import json
import os
import sys
from dataclasses import dataclass, field
from pathlib import Path


SOURCE_ROOT = Path(__file__).resolve().parent.parent
FROZEN = bool(getattr(sys, "frozen", False))
RESOURCE_ROOT = Path(getattr(sys, "_MEIPASS", SOURCE_ROOT))
APP_ROOT = Path(sys.executable).resolve().parent if FROZEN else SOURCE_ROOT


def _data_root() -> Path:
    explicit = os.environ.get("CINEFORGE_DATA_ROOT")
    if explicit:
        return Path(explicit)
    workstation_root = Path(r"X:\CineForge")
    if workstation_root.exists():
        return workstation_root / "data"
    return (Path(os.environ.get("LOCALAPPDATA", APP_ROOT)) / "CineForge") if FROZEN else APP_ROOT / "data"


DATA_ROOT = _data_root()
PROJECTS_ROOT = DATA_ROOT / "projects"
UPLOADS_ROOT = DATA_ROOT / "uploads"
LOGS_ROOT = DATA_ROOT / "logs"
GENERATED_ROOT = DATA_ROOT / "generated"


@dataclass
class Settings:
    host: str = "127.0.0.1"
    port: int = 7331
    comfy_url: str = "http://127.0.0.1:8188"
    inference_backend: str = "native"
    model_roots: list[str] = field(default_factory=list)
    model_cache_root: str | None = None
    output_root: str | None = None
    input_root: str | None = None


def _default_model_roots() -> list[str]:
    candidates = [
        os.environ.get("CINEFORGE_MODEL_ROOT", ""),
        r"X:\CineForge\models",
        r"A:\Shadowframe AI Local Distro\models",
        str(Path(os.environ.get("LOCALAPPDATA", "")) / "Shadowframe" / "Models"),
        str(Path.home() / ".cache" / "huggingface" / "hub"),
    ]
    return [str(Path(value)) for value in candidates if value and Path(value).exists()]


def load_settings() -> Settings:
    default_cache = Path(os.environ.get("CINEFORGE_MODEL_CACHE", r"X:\CineForge\models\.cache"))
    settings = Settings(model_roots=_default_model_roots(), model_cache_root=str(default_cache))
    config_path = DATA_ROOT / "config.json"
    fallback_config = APP_ROOT / "config.json"
    if not config_path.exists() and fallback_config.exists():
        config_path = fallback_config
    if config_path.exists():
        raw = json.loads(config_path.read_text(encoding="utf-8"))
        for key in ("host", "port", "comfy_url", "inference_backend", "model_roots", "model_cache_root", "output_root", "input_root"):
            if key in raw:
                setattr(settings, key, raw[key])
    PROJECTS_ROOT.mkdir(parents=True, exist_ok=True)
    UPLOADS_ROOT.mkdir(parents=True, exist_ok=True)
    LOGS_ROOT.mkdir(parents=True, exist_ok=True)
    GENERATED_ROOT.mkdir(parents=True, exist_ok=True)
    if settings.model_cache_root:
        Path(settings.model_cache_root).mkdir(parents=True, exist_ok=True)
    return settings
