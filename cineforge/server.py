from __future__ import annotations

import argparse
import base64
import json
import mimetypes
import os
import re
import logging
import threading
import urllib.parse
import urllib.request
import webbrowser
from http import HTTPStatus
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
from typing import Any

from .config import DATA_ROOT, FROZEN, LOGS_ROOT, PROJECTS_ROOT, RESOURCE_ROOT, UPLOADS_ROOT, Settings, load_settings
from .engine import NativeEngine
from .planner import build_plan


WEB_ROOT = RESOURCE_ROOT / "web"
SAFE_NAME = re.compile(r"[^a-zA-Z0-9._-]+")


def _save_project(project: dict[str, Any]) -> Path:
    path = PROJECTS_ROOT / f"{project['project_id']}.json"
    path.write_text(json.dumps(project, indent=2, ensure_ascii=False), encoding="utf-8")
    return path


class CineForgeServer(ThreadingHTTPServer):
    def __init__(self, address: tuple[str, int], settings: Settings):
        super().__init__(address, CineForgeHandler)
        self.settings = settings
        self.engine = NativeEngine(settings)

    def server_close(self) -> None:
        self.engine.close()
        super().server_close()


class CineForgeHandler(BaseHTTPRequestHandler):
    server: CineForgeServer

    def log_message(self, format: str, *args: Any) -> None:
        print(f"[CineForge] {self.address_string()} {format % args}")

    def _json(self, body: Any, status: int = 200) -> None:
        encoded = json.dumps(body, ensure_ascii=False).encode("utf-8")
        self.send_response(status)
        self.send_header("Content-Type", "application/json; charset=utf-8")
        self.send_header("Content-Length", str(len(encoded)))
        self.send_header("Cache-Control", "no-store")
        self.end_headers()
        self.wfile.write(encoded)

    def _error(self, exc: Exception, status: int = 500) -> None:
        self._json({"error": str(exc), "type": exc.__class__.__name__}, status)

    def _body(self) -> dict[str, Any]:
        length = int(self.headers.get("Content-Length", "0"))
        if length > 60 * 1024 * 1024:
            raise ValueError("Request is larger than the 60 MB local upload limit.")
        raw = self.rfile.read(length)
        return json.loads(raw.decode("utf-8")) if raw else {}

    def do_GET(self) -> None:
        parsed = urllib.parse.urlparse(self.path)
        try:
            if parsed.path == "/api/health":
                status = self.server.engine.runtime()
                self._json({"app": "CineForge Local", "version": "0.2.0", "runtime": status})
                return
            if parsed.path == "/api/models":
                self._json(self.server.engine.models())
                return
            if parsed.path.startswith("/api/jobs/"):
                prompt_id = parsed.path.rsplit("/", 1)[-1]
                self._json(self.server.engine.history(prompt_id))
                return
            if parsed.path == "/api/media":
                query = urllib.parse.parse_qs(parsed.query)
                media_path = self.server.engine.media_path(query.get("path", [""])[0])
                payload = media_path.read_bytes()
                self.send_response(200)
                self.send_header("Content-Type", mimetypes.guess_type(media_path.name)[0] or "application/octet-stream")
                self.send_header("Content-Length", str(len(payload)))
                self.send_header("Cache-Control", "private, max-age=3600")
                self.end_headers()
                self.wfile.write(payload)
                return
            if parsed.path.startswith("/api/projects/"):
                project_id = SAFE_NAME.sub("", parsed.path.rsplit("/", 1)[-1])
                project_path = PROJECTS_ROOT / f"{project_id}.json"
                if not project_path.exists():
                    self._json({"error": "Project not found"}, 404)
                else:
                    self._json(json.loads(project_path.read_text(encoding="utf-8")))
                return
            self._static(parsed.path)
        except Exception as exc:
            self._error(exc)

    def do_POST(self) -> None:
        parsed = urllib.parse.urlparse(self.path)
        try:
            body = self._body()
            if parsed.path == "/api/plan":
                project = build_plan(body)
                _save_project(project)
                self._json(project, HTTPStatus.CREATED)
                return
            if parsed.path == "/api/projects/save":
                if not body.get("project_id"):
                    raise ValueError("project_id is required")
                _save_project(body)
                self._json({"saved": True, "project_id": body["project_id"]})
                return
            if parsed.path == "/api/shutdown":
                self._json({"stopping": True})
                threading.Thread(target=self.server.shutdown, daemon=True).start()
                return
            if parsed.path == "/api/upload":
                name = SAFE_NAME.sub("_", str(body.get("name") or "reference.png"))
                content = str(body.get("data") or "")
                if "," in content:
                    content = content.split(",", 1)[1]
                raw = base64.b64decode(content, validate=True)
                if len(raw) > 50 * 1024 * 1024:
                    raise ValueError("Reference file exceeds 50 MB.")
                path = UPLOADS_ROOT / name
                path.write_bytes(raw)
                engine_result = self.server.engine.upload_image(path)
                self._json({"saved": str(path), "asset": engine_result}, HTTPStatus.CREATED)
                return
            if parsed.path == "/api/render/still":
                result = self.server.engine.queue_still(
                    prompt=str(body.get("prompt") or ""),
                    negative_prompt=str(body.get("negative_prompt") or ""),
                    model_id=str(body.get("model_id") or "moody-real"),
                    width=int(body.get("width") or 768), height=int(body.get("height") or 432),
                    seed=body.get("seed"), reference_image=body.get("reference_image"),
                    quality=str(body.get("quality") or "proof"),
                )
                self._json(result, HTTPStatus.ACCEPTED)
                return
            if parsed.path == "/api/render/video":
                media = body.get("image") if isinstance(body.get("image"), dict) else None
                image_path = self.server.engine.prepare_video_input(media) if media else str(body.get("image_path") or "")
                if not image_path:
                    raise ValueError("image_path is required for image-to-video")
                result = self.server.engine.queue_video(
                    image_path=str(image_path), prompt=str(body.get("prompt") or ""),
                    negative_prompt=str(body.get("negative_prompt") or ""),
                    width=int(body.get("width") or 768), height=int(body.get("height") or 432),
                    length=int(body.get("length") or 17), seed=body.get("seed"),
                    quality=str(body.get("quality") or "proof"),
                    model_id=str(body.get("model_id") or "wan2.2"),
                )
                self._json(result, HTTPStatus.ACCEPTED)
                return
            self._json({"error": "Endpoint not found"}, 404)
        except (ValueError, json.JSONDecodeError) as exc:
            self._error(exc, 400)
        except Exception as exc:
            self._error(exc)

    def _static(self, request_path: str) -> None:
        relative = "index.html" if request_path in {"", "/"} else request_path.lstrip("/")
        candidate = (WEB_ROOT / relative).resolve()
        if WEB_ROOT.resolve() not in candidate.parents and candidate != WEB_ROOT.resolve():
            self._json({"error": "Invalid path"}, 400)
            return
        if not candidate.exists() or not candidate.is_file():
            candidate = WEB_ROOT / "index.html"
        payload = candidate.read_bytes()
        self.send_response(200)
        self.send_header("Content-Type", mimetypes.guess_type(candidate.name)[0] or "application/octet-stream")
        self.send_header("Content-Length", str(len(payload)))
        self.end_headers()
        self.wfile.write(payload)


def main() -> None:
    parser = argparse.ArgumentParser(description="Run CineForge Local")
    parser.add_argument("--host")
    parser.add_argument("--port", type=int)
    parser.add_argument("--no-browser", action="store_true")
    args = parser.parse_args()
    settings = load_settings()
    if args.host:
        settings.host = args.host
    if args.port:
        settings.port = args.port
    url = f"http://{settings.host}:{settings.port}"
    try:
        with urllib.request.urlopen(f"{url}/api/health", timeout=1) as response:
            existing = json.loads(response.read().decode("utf-8"))
        if existing.get("app") == "CineForge Local":
            webbrowser.open(url)
            return
    except Exception:
        pass
    logging.basicConfig(
        filename=str(LOGS_ROOT / "cineforge.log") if FROZEN else None,
        level=logging.INFO,
        format="%(asctime)s %(levelname)s %(message)s",
    )
    try:
        server = CineForgeServer((settings.host, settings.port), settings)
    except OSError as exc:
        message = f"CineForge could not start on {settings.host}:{settings.port}.\n\n{exc}"
        if os.name == "nt":
            import ctypes
            ctypes.windll.user32.MessageBoxW(0, message, "CineForge Local", 0x10)
        raise
    logging.info("CineForge Local started at %s with the native CineForge Engine", url)
    if not args.no_browser:
        webbrowser.open(url)
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        pass
    finally:
        server.server_close()
        logging.info("CineForge Local stopped")


if __name__ == "__main__":
    main()
