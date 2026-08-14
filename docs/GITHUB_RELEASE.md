# CineForge Desktop v0.4.0 — Native Wan Release Candidate

CineForge runs through its own PyTorch/Diffusers inference engine. ComfyUI is not launched, contacted, or required by the application.

## Highlights

- Native Windows installer with separate application and CineForge Library selectors.
- Automatic, resumable Wan model download from the CineForge Hugging Face repository.
- SHA-256 verification before the model pack is marked complete.
- Dedicated inputs, outputs, projects, models, cache, logs, and temporary folders.
- Direct NVIDIA GPU and VRAM telemetry.
- Native local job queue with live steps, phases, elapsed time, ETA, and errors.
- Standalone Diffusers model-pack discovery.
- Five camera angles, five inserts, and five story-progressing prompts per sequence.
- Reference-pack uploads and continuity selection.
- Existing raw checkpoint files are preserved and identified as conversion candidates.

## Download

Download `CineForge-Desktop-Setup-0.4.0-win-x64.exe` and use `SHA256SUMS.txt` to verify it.

The setup EXE is intentionally small. During installation it downloads the matching `CineForge-Desktop-Runtime-0.4.0-win-x64.zip` release asset, verifies its SHA-256 checksum, extracts the application, and then downloads the pinned Wan pack. Publish both assets in the same GitHub Release.

### Verified preview artifact

- Product: **CineForge Desktop**
- Edition: `desktop`
- Version: `0.4.0`
- Architecture: Windows x64
- Bootstrap installer size: 50,388,418 bytes
- Bootstrap installer SHA-256: `4d9a60caa8651c4c0dd18d95c7ea05beededd7d0744d83509892aa4a7d892d60`
- Runtime archive size: 2,017,201,506 bytes
- Runtime archive SHA-256: `4ab5dbfbe10c576aea97276f3e8a554d57fe544d0cf11437cb656cec4794d346`
- Model payload: four Wan components totaling 35,579,207,879 bytes
- Model revision: `493b7c8ff0a451b6b4c049afb3e6396dbfa1c688`

## Desktop installation experience

1. The user chooses the CineForge Desktop application location.
2. The user chooses a separate CineForge Library location for models and generated media.
3. Setup checks that the selected library drive has enough free space.
4. Setup automatically downloads the required Wan model components from the pinned CineForge Hugging Face revision.
5. Interrupted transfers are preserved as `.partial` files and resume when setup is run again.
6. Every component is checked by file size and SHA-256 before the model pack is accepted.
7. Setup creates isolated inputs, outputs, projects, models, cache, logs, and temporary directories inside the selected library.

No manual model placement is required. CineForge Desktop does not reuse Shadowframe's models, input folders, output folders, or runtime.

## Model note

The CineForge Desktop installer contains the CineForge Engine but not the large model weights. During setup it automatically downloads the four required Wan components plus the small scheduler, tokenizer, and architecture support files from [TheBaldDudeCo/CineForge-Wan-Models](https://huggingface.co/TheBaldDudeCo/CineForge-Wan-Models) into the selected CineForge Library. Users do not manually locate or install model files. Version 0.4.0 pins immutable model revision `493b7c8ff0a451b6b4c049afb3e6396dbfa1c688`.

## Security and signing

CineForge binds only to `127.0.0.1` and stores user data locally. It never scans or writes to Shadowframe folders. The prerelease build is unsigned; apply an Authenticode signature before a broad public release.

## Preview status

This installer is an unsigned release candidate. Native split-Wan loading, real end-to-end generation, and MP4 output validation have passed on an RTX 4070 12 GB system. Do not label it as the stable public release until isolated model-download installation, clean-machine install/uninstall, and production signing have passed.
