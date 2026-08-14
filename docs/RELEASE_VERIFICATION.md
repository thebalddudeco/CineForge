# CineForge 0.3.0 preview verification

Verified on 2026-08-14 on Windows 11 with an NVIDIA GeForce RTX 4070.

## Model distribution

- Repository: `TheBaldDudeCo/CineForge-Wan-Models`
- Immutable revision: `3abefe070febb87cf51e038edda29934743639fb`
- Four core components published: 35,579,207,879 bytes
- Remote byte sizes and SHA-256-backed Hugging Face content identifiers: passed
- Installer model revision pin: passed

## Packaged runtime

- Python: 3.12
- PyTorch: `2.10.0+cu130`
- Diffusers: `0.39.0`
- Transformers: `5.0.0`
- Backend: native CineForge engine
- ComfyUI dependency: none
- Shadowframe runtime or directory dependency: none

## Build checks

- Independent CUDA packaging environment created on `X:`: passed
- PyInstaller analysis and standalone application bundle: passed
- Native Windows installer compilation: passed
- Installer file version: `0.3.0.0`
- Installer size: 2,033,987,862 bytes
- SHA-256: `c7e41d44f08f9990722461fd7c65cdfad27866440736bd3540a7d015a61a2ffa`
- Generated checksum comparison: passed
- Automated tests: 13 passed
- Authenticode: not signed

## Remaining preview gates

- Native loading of the split Wan scaled-FP8 pack
- Real end-to-end Wan generation without ComfyUI
- Output and media-probe validation
- Installer model-download test in an isolated library
- Clean-machine installation and uninstall test
- Authenticode production signing

This artifact is a technical preview and must not be represented as a stable public release until the remaining gates pass.
