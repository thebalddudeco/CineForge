from __future__ import annotations

import json
import logging
import re
import shutil
import sys
from pathlib import Path
from typing import Any

from .config import FROZEN, LOGS_ROOT, PROJECTS_ROOT, UPLOADS_ROOT, load_settings
from .engine import NativeEngine
from .planner import build_plan


SAFE_NAME = re.compile(r"[^a-zA-Z0-9._-]+")


def _save_project(project: dict[str, Any]) -> Path:
    project_id = SAFE_NAME.sub("", str(project.get("project_id") or ""))
    if not project_id:
        raise ValueError("project_id is required")
    path = PROJECTS_ROOT / f"{project_id}.json"
    path.write_text(json.dumps(project, indent=2, ensure_ascii=False), encoding="utf-8")
    return path


class Worker:
    """Private native-desktop command worker. It never binds a network port."""

    def __init__(self) -> None:
        self.engine = NativeEngine(load_settings())
        self.stopping = False

    def close(self) -> None:
        self.engine.close()

    def dispatch(self, command: str, payload: dict[str, Any]) -> Any:
        if command == "health":
            return {
                "app": "CineForge Desktop",
                "edition": "desktop",
                "version": "0.5.0",
                "transport": "private-process",
                "runtime": self.engine.runtime(),
            }
        if command == "models":
            return self.engine.models()
        if command == "plan":
            project = build_plan(payload)
            _save_project(project)
            return project
        if command == "save_project":
            path = _save_project(payload)
            return {"saved": True, "project_id": payload["project_id"], "path": str(path)}
        if command == "open_project":
            project_id = SAFE_NAME.sub("", str(payload.get("project_id") or ""))
            path = PROJECTS_ROOT / f"{project_id}.json"
            if not path.is_file():
                raise FileNotFoundError("Project not found")
            return json.loads(path.read_text(encoding="utf-8"))
        if command == "import_reference":
            source = Path(str(payload.get("path") or "")).expanduser().resolve()
            if not source.is_file():
                raise FileNotFoundError("The selected reference image was not found.")
            if source.stat().st_size > 50 * 1024 * 1024:
                raise ValueError("Reference file exceeds 50 MB.")
            name = SAFE_NAME.sub("_", str(payload.get("name") or source.name))
            destination = UPLOADS_ROOT / name
            destination.parent.mkdir(parents=True, exist_ok=True)
            if source != destination.resolve():
                shutil.copy2(source, destination)
            return {"saved": str(destination), "asset": self.engine.upload_image(destination)}
        if command == "queue_video":
            return self.engine.queue_video(**payload)
        if command == "job":
            return self.engine.history(str(payload.get("job_id") or ""))
        if command == "shutdown":
            self.stopping = True
            return {"stopping": True}
        raise ValueError(f"Unknown desktop command: {command}")


def main() -> None:
    LOGS_ROOT.mkdir(parents=True, exist_ok=True)
    logging.basicConfig(
        filename=str(LOGS_ROOT / "cineforge-engine.log"),
        level=logging.INFO,
        format="%(asctime)s %(levelname)s %(message)s",
    )
    worker = Worker()
    try:
        for line in sys.stdin:
            cleaned = line.strip().lstrip("\ufeffï»¿")
            if not cleaned:
                continue
            request_id: Any = None
            try:
                request = json.loads(cleaned)
                request_id = request.get("request_id")
                result = worker.dispatch(str(request.get("command") or ""), request.get("payload") or {})
                response = {"request_id": request_id, "ok": True, "result": result}
            except Exception as exc:
                logging.exception("Native worker command failed")
                response = {
                    "request_id": request_id,
                    "ok": False,
                    "error": str(exc),
                    "error_type": exc.__class__.__name__,
                }
            sys.stdout.write(json.dumps(response, ensure_ascii=False) + "\n")
            sys.stdout.flush()
            if worker.stopping:
                break
    finally:
        worker.close()


if __name__ == "__main__":
    main()
