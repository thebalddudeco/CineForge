# Changelog

All notable CineForge changes are recorded here. The format follows Keep a Changelog and versions follow Semantic Versioning.

## [Unreleased]

### Changed

- Narrowed CineForge to a dedicated Wan video-generation application.
- Removed LTX and built-in still-image generation from the supported product scope.
- Defined source-image/keyframe import as the entry point for every generation.
- Established separate GitHub application and Hugging Face model distribution tracks.
- Added mandatory release, provenance, compatibility, verification, and decision records.
- Matched the local Wan 2.2 FP8 experts, UMT5 encoder, VAE, and optional acceleration LoRAs byte-for-byte to immutable upstream Hugging Face artifacts.

### In progress

- Native loading of the split Wan 2.2 I2V A14B scaled-FP8 model pack.
- Real end-to-end generation on a 12 GB NVIDIA GPU without ComfyUI.
- Conversion and validation tooling for a redistributable CineForge Wan pack.

## [0.2.0] - 2026-08-14

### Added

- Standalone native Python/PyTorch application runtime.
- GPU and VRAM detection without contacting ComfyUI.
- Native job queue with generation phase, step progress, elapsed time, ETA, output, and error telemetry.
- Windows x64 installer build and clean-process shutdown behavior.
- Futuristic runtime signal history, dot-matrix generation monitor, and live progress frame.

### Verified

- Frozen Windows application launched with ComfyUI stopped.
- Bundled PyTorch, Diffusers, and Transformers components loaded successfully.
- CUDA diagnostic inference completed and saved an output locally.

### Known limitations

- This was an engineering preview, not a supported Wan production release.
- Raw split/scaled-FP8 Wan assets were detected but not yet validated through the standalone native adapter.
- The UI and API still contained earlier still-image workflow surfaces.

## [0.1.0] - 2026-08-14

### Added

- Initial FLORA-inspired cinematic workflow prototype.
- Canon/reference pack, five-angle, five-insert, and five-story-progression branches.
- Continuity selection, motion prompts, local model discovery, and Windows packaging prototype.

### Deprecated

- ComfyUI-backed execution and multi-model routing were superseded by the standalone Wan-only direction.

[Unreleased]: https://github.com/thebalddudeco/CineForge/compare/v0.2.0...HEAD
[0.2.0]: https://github.com/thebalddudeco/CineForge/releases/tag/v0.2.0
[0.1.0]: https://github.com/thebalddudeco/CineForge/releases/tag/v0.1.0
