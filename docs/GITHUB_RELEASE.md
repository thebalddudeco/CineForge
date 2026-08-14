# CineForge Desktop v0.3.1 — Isolated Wan Installer Preview

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

Download `CineForge-Desktop-Setup-0.3.1-win-x64.exe` and use `SHA256SUMS.txt` to verify it.

### Verified preview artifact

- Product: **CineForge Desktop**
- Edition: `desktop`
- Version: `0.3.1`
- Architecture: Windows x64
- Installer size: 2,033,988,934 bytes
- SHA-256: `ec231da71272a0e9af14f738e09b925091cce5634ac5c085c4dad62590caacf5`
- Model payload: four Wan components totaling 35,579,207,879 bytes
- Model revision: `3abefe070febb87cf51e038edda29934743639fb`

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

The CineForge Desktop installer contains the CineForge Engine but not the large model weights. During setup it automatically downloads the four required Wan components from [TheBaldDudeCo/CineForge-Wan-Models](https://huggingface.co/TheBaldDudeCo/CineForge-Wan-Models) into the selected CineForge Library. Users do not manually locate or install model files. Version 0.3.1 pins immutable model revision `3abefe070febb87cf51e038edda29934743639fb`.

## Security and signing

CineForge binds only to `127.0.0.1` and stores user data locally. It never scans or writes to Shadowframe folders. The prerelease build is unsigned; apply an Authenticode signature before a broad public release.

## Preview status

This installer is an unsigned technical preview. Do not label it as the stable public release until native split-Wan loading, a real end-to-end generation, output validation, isolated model-download installation, clean-machine install/uninstall, and production signing have passed.
