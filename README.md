# CineForge

CineForge is a Wan-powered cinematic video system with two planned editions. Both editions share the same visual language, project structure, and generation workflow.

- **CineForge Desktop** is the private, local-only Windows edition. It downloads the required Wan models and performs generation on the user's own compatible NVIDIA GPU.
- **CineForge Online** is the lightweight, responsive browser and mobile edition. It sends approved generation jobs through a secure CineForge API broker to third-party video-model APIs, so phones and laptops download no model weights and need no dedicated GPU. The application is planned to launch as a free-to-access beta; users connect and fund their own supported provider account for generation. CineForge does not subsidize provider inference.

The current repository and installer work are focused on CineForge Desktop.

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
- No application-level prompt or output content moderation in CineForge Desktop

The Desktop edition is a local creative tool: prompts, imported media, and generated outputs stay under the user's control. Users remain responsible for complying with applicable law and respecting consent, privacy, likeness, and intellectual-property rights.

See [docs/PRODUCT_EDITIONS.md](docs/PRODUCT_EDITIONS.md) for the shared product contract and edition-specific runtime boundaries.

CineForge Online is an SFW-only hosted service. Prompts, uploaded reference media, and generated outputs must pass the layered moderation contract in [docs/ONLINE_MODERATION.md](docs/ONLINE_MODERATION.md).

## Repository map

- `cineforge/` — native local server, model discovery, job orchestration, and project planning
- `web/` — desktop application interface
- `packaging/` — Windows installer build and signing scripts
- `tests/` — automated checks
- `docs/` — architecture, compatibility, release, provenance, and decision records
- `.github/` — issue templates, pull-request template, and release automation

## Run CineForge Desktop from source

Desktop development currently requires Windows 11, Python 3.12, an NVIDIA CUDA-capable GPU, and a compatible CineForge Wan model pack. These hardware requirements do not apply to people using CineForge Online; Online generation compute is supplied by its configured third-party model provider.

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
