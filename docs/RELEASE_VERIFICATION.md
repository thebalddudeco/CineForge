# CineForge Desktop 0.5.0 native-desktop release-candidate verification

Verified on 2026-08-14 on Windows 11 with an NVIDIA GeForce RTX 4070.

## Desktop identity

- Windows product name: `CineForge Desktop`
- Setup filename: `CineForge-Desktop-Setup-0.5.0-win-x64.exe`
- Installed Apps display name: `CineForge Desktop`
- Desktop and Start Menu shortcut name: `CineForge Desktop`
- Application folder: `CineForge`
- Data folder: `CineForge Library`

## Model distribution

- Automatic setup download: enabled
- Manual model placement required: no
- Repository: `TheBaldDudeCo/CineForge-Wan-Models`
- Immutable revision: `493b7c8ff0a451b6b4c049afb3e6396dbfa1c688`
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

## Prior local build checks

- Independent CUDA packaging environment on `X:`: passed
- PyInstaller standalone application build: passed
- Native Windows installer compilation: passed
- Installer product name: `CineForge Desktop`
- Installer file version: `0.5.0.0`
- The earlier native packaging proof produced a complete installer and matching checksum; the tagged v0.5.0 workflow rebuilds the installer and runtime from the release commit.
- Exact tagged installer/runtime byte sizes and SHA-256 values are authoritative only in the v0.5.0 release assets `CineForge-Release.json` and `SHA256SUMS.txt`.
- Automated tests: 21 passed
- Authenticode: not signed

## Native generation verification

- Native loading of the split Wan scaled-FP8 pack: passed
- Real two-expert end-to-end Wan generation without ComfyUI: passed
- Finite latents, finite non-black frames, live progress telemetry, MP4 export, and media-probe validation: passed

## Remaining stable-release gates

- Installer model-download test in an isolated library
- Clean-machine installation and uninstall test
- Authenticode production signing

The tagged v0.5.0 artifact is an unsigned public prerelease. It must not be represented as a stable release until the remaining gates pass.
