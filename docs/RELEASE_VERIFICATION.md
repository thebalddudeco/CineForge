# CineForge Desktop 0.3.1 preview verification

Verified on 2026-08-14 on Windows 11 with an NVIDIA GeForce RTX 4070.

## Desktop identity

- Windows product name: `CineForge Desktop`
- Setup filename: `CineForge-Desktop-Setup-0.3.1-win-x64.exe`
- Installed Apps display name: `CineForge Desktop`
- Desktop and Start Menu shortcut name: `CineForge Desktop`
- Application folder: `CineForge`
- Data folder: `CineForge Library`

## Model distribution

- Automatic setup download: enabled
- Manual model placement required: no
- Repository: `TheBaldDudeCo/CineForge-Wan-Models`
- Immutable revision: `3abefe070febb87cf51e038edda29934743639fb`
- Four core components: 35,579,207,879 bytes
- Resumable partial transfers: enabled
- Per-file SHA-256 verification: enabled
- Remote byte sizes and content identifiers: passed

## Packaged runtime

- Python: 3.12
- PyTorch: `2.10.0+cu130`
- Diffusers: `0.39.0`
- Transformers: `5.0.0`
- Backend: native CineForge engine
- ComfyUI dependency: none
- Shadowframe runtime or directory dependency: none

## Build checks

- Independent CUDA packaging environment on `X:`: passed
- PyInstaller standalone application build: passed
- Native Windows installer compilation: passed
- Installer product name: `CineForge Desktop`
- Installer file version: `0.3.1.0`
- Installer size: 2,033,988,934 bytes
- SHA-256: `ec231da71272a0e9af14f738e09b925091cce5634ac5c085c4dad62590caacf5`
- Generated checksum comparison: passed
- Automated tests: 14 passed
- Authenticode: not signed

## Remaining preview gates

- Native loading of the split Wan scaled-FP8 pack
- Real end-to-end Wan generation without ComfyUI
- Output and media-probe validation
- Installer model-download test in an isolated library
- Clean-machine installation and uninstall test
- Authenticode production signing

This artifact is a technical preview and must not be represented as a stable public release until the remaining gates pass.
