# CineForge Desktop v0.5.0 — Native Windows Release Candidate

CineForge now runs as a genuine native WPF Windows application with its own private PyTorch/Diffusers inference worker. It does not launch a browser, host a localhost website, embed a WebView, launch ComfyUI, or contact ComfyUI.

## What’s new in this drop

- Full native-desktop conversion with a private local engine process.
- Major CineForge brand-system rebuild based on the approved v0.1.0 visual language and retrofuturist micrographic references.
- Approved tapered version badge now sourced from the locked Brand System System artboard and updated to `v0.5.0` for app and installer use.
- Native branded installer redesign with CineForge chrome, segmented installation telemetry, clipped frames, and offline bundled UI fonts.
- Live runtime signal history panel for GPU / VRAM / engine / build state.
- Native breathing 38 × 10 generation matrix with separate real progress rail and live job telemetry.
- Persistent English, Korean, and Japanese localization with approved fonts.
- Top-level CineForge brand library consolidated for logos, exports, motion studies, references, proofs, and brand docs.

## Highlights

- Native Windows installer with separate application and CineForge Library selectors.
- Native WPF workflow interface and private child-process engine transport.
- Automatic, resumable Wan model download from the CineForge Hugging Face repository.
- SHA-256 verification before the model pack is marked complete.
- Dedicated inputs, outputs, projects, models, cache, logs, and temporary folders.
- Direct NVIDIA GPU and VRAM telemetry.
- Native local job queue with live steps, phases, elapsed time, ETA, and errors.
- Standalone Diffusers model-pack discovery.
- Five camera angles, five inserts, and five story-progressing prompts per sequence.
- Reference-pack uploads and continuity selection.
- Existing raw checkpoint files are preserved and identified as conversion candidates.

## Brand and design-system notes

- The current interface is no longer the flat interim native shell; it now follows the locked CineForge visual contract.
- The desktop app and installer both use the approved chartreuse / carbon / black / alabaster / lime palette with enforced contrast-safe state behavior.
- The runtime header, generation monitor, installation signal, and workflow framing now reuse the same clipped technical grammar.
- CineForge’s brand assets are now organized into a consolidated local `Brand System` archive containing source AI files, SVGs, transparent PNG masters, export sizes, favicons, fonts, motion studies, proof sheets, and reference boards.

## Download

Download `CineForge-Desktop-Setup-0.5.0-win-x64.exe` and use `SHA256SUMS.txt` to verify it.

New users should follow the complete **[CineForge Desktop 0.5.0 User Guide](https://github.com/thebalddudeco/CineForge/blob/main/docs/USER_GUIDE.md)**. It explains installation, the Scene Brief, canonical reference lock, 15-shot factory, candidate-level Generate Video controls, live progress data, outputs, localization, current version behavior, and troubleshooting.

The setup EXE is intentionally small. During installation it downloads the matching `CineForge-Desktop-Runtime-0.5.0-win-x64.zip` release asset, verifies its SHA-256 checksum, extracts the application, and then downloads the pinned Wan pack. Publish both assets in the same GitHub Release.

### Verified preview artifact

- Product: **CineForge Desktop**
- Edition: `desktop`
- Version: `0.5.0`
- Architecture: Windows x64
- Exact installer and runtime sizes and SHA-256 values: included with each build in `CineForge-Release.json` and `SHA256SUMS.txt`
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

The CineForge Desktop installer contains the native CineForge interface and private CineForge Engine but not the large model weights. During setup it automatically downloads the required Wan components plus the scheduler, tokenizer, and architecture support files from [TheBaldDudeCo/CineForge-Wan-Models](https://huggingface.co/TheBaldDudeCo/CineForge-Wan-Models) into the selected CineForge Library. Users do not manually locate or install model files. Version 0.5.0 pins immutable model revision `493b7c8ff0a451b6b4c049afb3e6396dbfa1c688`.

## Security and signing

CineForge Desktop opens no listening port and stores user data locally. It never scans or writes to Shadowframe folders. The prerelease build is unsigned; apply an Authenticode signature before a broad public release.

## Preview status

This installer is an unsigned release candidate. Native split-Wan loading, real end-to-end generation, and MP4 output validation have passed on an RTX 4070 12 GB system. Do not label it as the stable public release until isolated model-download installation, clean-machine install/uninstall, and production signing have passed.
