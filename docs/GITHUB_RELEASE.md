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

## Model note

The CineForge Desktop installer contains the CineForge Engine but not the large model weights. During setup it automatically downloads the four required Wan components from [TheBaldDudeCo/CineForge-Wan-Models](https://huggingface.co/TheBaldDudeCo/CineForge-Wan-Models) into the selected CineForge Library. Users do not manually locate or install model files. Version 0.3.1 pins immutable model revision `3abefe070febb87cf51e038edda29934743639fb`.

## Security and signing

CineForge binds only to `127.0.0.1` and stores user data locally. It never scans or writes to Shadowframe folders. The prerelease build is unsigned; apply an Authenticode signature before a broad public release.
