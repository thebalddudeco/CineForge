# CineForge Local v0.2.0 — Native Engine Preview

CineForge now runs through its own PyTorch/Diffusers inference engine. ComfyUI is no longer launched, contacted, or required by the application.

## Highlights

- Native Windows installer with bundled inference libraries.
- Direct NVIDIA GPU and VRAM telemetry.
- Native local job queue with live steps, phases, elapsed time, ETA, and errors.
- Standalone Diffusers model-pack discovery.
- Five camera angles, five inserts, and five story-progressing prompts per sequence.
- Reference-pack uploads and continuity selection.
- Existing raw checkpoint files are preserved and identified as conversion candidates.

## Download

Download `CineForge-Setup-0.2.0-win-x64.exe` and use `SHA256SUMS.txt` to verify it.

## Model note

The installer contains the CineForge Engine but not large third-party model weights. Generation requires a compatible standalone model pack. The existing Wan 2.2, Anima, RedCraft, Moody Real, LTX, and Qwen assets on the development workstation remain untouched; workflow-specific or scaled-FP8 files are not marked runnable until their native adapters are verified.

## Security and signing

CineForge binds only to `127.0.0.1` and stores user data locally. The prerelease build is unsigned; apply an Authenticode signature before a broad public release.
