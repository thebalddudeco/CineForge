# CineForge Desktop

CineForge Desktop is a Windows application for locally generating cinematic video with Wan. It is being rebuilt as a focused, standalone Wan image-to-video workflow: import a source frame, direct motion and camera behavior, generate locally, monitor real progress, preview, and export.

## Project status

**Pre-alpha / native Wan integration in progress.**

The native application shell, local GPU runtime, job telemetry, project workflow, Windows packaging, and installer have been exercised independently of ComfyUI. The production Wan FP8 loader is the current engineering milestone. The previously built 0.2.0 installer is an engineering preview and is not the first supported public release.

Do not interpret the presence of raw Wan files as proof that a standalone pack is runnable. A model pack becomes supported only after it passes the validation gates documented in [docs/MODEL_PACK_SPEC.md](docs/MODEL_PACK_SPEC.md).

## Product scope

- Wan-only local video generation
- Source-image/keyframe input
- Motion, action, and camera-direction prompting
- Duration, resolution, frame rate, quality, and seed controls
- Real-time stage, percentage, elapsed-time, ETA, and GPU/VRAM telemetry
- Local preview and export
- No ComfyUI service dependency
- No built-in still-image generator
- No LTX runtime

## Repository map

- `cineforge/` — native local server, model discovery, job orchestration, and project planning
- `web/` — desktop application interface
- `packaging/` — Windows installer build and signing scripts
- `tests/` — automated checks
- `docs/` — architecture, compatibility, release, provenance, and decision records
- `.github/` — issue templates, pull-request template, and release automation

## Run from source

Requirements for development currently include Windows 11, Python 3.12, an NVIDIA CUDA-capable GPU, and a compatible CineForge Wan model pack.

```powershell
.\run.ps1
```

CineForge opens locally at `http://127.0.0.1:7331`.

## Verify

```powershell
python -m unittest discover -s tests -v
python -m cineforge.server --no-browser
```

## Releases and paper trail

Every user-facing release must include:

- a SemVer tag and immutable GitHub Release;
- a changelog entry;
- release notes with known limitations;
- installer SHA-256 checksums;
- model-pack compatibility and provenance records;
- verification evidence for supported hardware and workflows.

The required process is documented in [docs/RELEASE_PROCESS.md](docs/RELEASE_PROCESS.md). Architecture and scope decisions are recorded in [docs/decisions](docs/decisions).

## Models

Model weights are distributed separately through [TheBaldDudeCo/CineForge-Wan-Models](https://huggingface.co/TheBaldDudeCo/CineForge-Wan-Models). The application repository never stores model weights. The CineForge Desktop installer asks for separate application and CineForge Library locations, then automatically downloads every required Wan component into the selected library with resumable transfers and SHA-256 verification. Users do not need to find or place model files manually. See [docs/MODEL_PROVENANCE.md](docs/MODEL_PROVENANCE.md) and [docs/MODEL_PACK_SPEC.md](docs/MODEL_PACK_SPEC.md).

The CineForge Library is isolated from every other application and contains `inputs`, `outputs`, `projects`, `models`, `cache`, `logs`, and `temp`. CineForge does not scan or write to Shadowframe directories.

Wan 2.2 is an upstream project by the Wan Team. CineForge is an independent application and is not affiliated with or endorsed by Alibaba or the Wan Team.

## License

CineForge application source is licensed under the [Apache License 2.0](LICENSE). Third-party models, libraries, fonts, and visual assets remain subject to their respective licenses.
