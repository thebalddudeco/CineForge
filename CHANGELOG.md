# Changelog

All notable CineForge changes are recorded here. The format follows Keep a Changelog and versions follow Semantic Versioning.

## [Unreleased]

## [0.5.0] - 2026-08-15

### Native Windows interface and brand-system implementation

- Rebuilt CineForge Desktop as a genuine native WPF application with no browser window, localhost server, embedded web view, or ComfyUI process.
- Ported the complete cinematic workflow into native controls: scene brief, continuity/reference pack, 15-shot factory, model selection, generation controls, progress telemetry, elapsed time, ETA, and output access.
- Implemented the approved v0.1.0-derived retrofuturist visual system with the locked five-color palette (`#E4FF1A`, `#242424`, `#020300`, `#E0E0E0`, and `#89FC00`).
- Added clipped and tapered instrument frames, one-pixel ivory rules, selective chartreuse activation, corner registration marks, technical labels, segmented signals, sparse grid structure, restrained stationary grain, and operational data callouts.
- Restored the approved filled tapered six-sided version badge with dark version text and uniform scaling.
- Added bundled offline English, Korean, and Japanese interface dictionaries and the approved language-specific font families, plus persistent three-control language switching.
- Enforced the contrast contract: dark text on light/green surfaces and light text on dark surfaces; removed generic blue focus and selection styling.
- Added a live native GPU/VRAM runtime history display and four-trace diagnostic styling driven by current runtime samples.
- Added the live 38 × 10 generation matrix: fixed cell positions, common five-second breathing cycle, asynchronous phases, migrating restrained acid-lime signals, and a separate real progress rail.
- Packaged the user-approved orbital-globe GIF as a native application resource, preserved its source timing and frames, reduced its header presentation to 86% to prevent clipping, and corrected the lower-right registration mark orientation.
- Reduced stationary film-grain prominence by 35% and added dithering to minimize visible gradient banding without animating the page texture.
- Removed the obsolete browser UI, localhost server, ComfyUI adapter, and legacy ComfyUI workflow file from the Desktop distribution.
- Added a private stdin/stdout transport between the native WPF interface and the hidden local Wan worker.
- Updated installer, runtime packaging, automatic model delivery, localization persistence, release verification, and architecture documentation for the native Desktop layout.
- Moved the WAN model selector beneath Clip Length, removed the oversized model-router panel, and moved Build 15-Shot Factory beneath the reference slots so the visible control order matches the required workflow.
- Disabled the shot-factory action until a canonical reference image is locked and automatically reveals the generated candidates after planning.

### Documentation and traceability

- Added the locked visual contract and the archived v0.1.0 visual source-of-truth record.
- Added a complete product history from v0.1.0 through v0.5.0.
- Added an implementation record mapping approved visual decisions to production components and documenting superseded concepts.
- Stamped the application, engine, installer, packaging defaults, localized titles, telemetry, and approved version badge consistently as `0.5.0`.

### Changed

- Replaced the browser-hosted Desktop interface with a genuine native Windows WPF application for v0.5.0.
- Removed the localhost HTTP server, browser launcher, static web client, ComfyUI adapter, and legacy workflow JSON from the Desktop runtime.
- Added a private stdin/stdout command channel between the native interface and the bundled hidden Wan engine process.
- Rebuilt the scene brief, reference lock, 15-shot factory, Wan model selector, generation controls, live dot-matrix monitor, percentage, phase, elapsed time, ETA, and output launch behavior as native Windows controls.
- Changed release packaging so `CineForge.exe` is the native WPF application and `Engine/CineForge Engine.exe` is its non-networked inference worker.
- Restored the approved retrofuturist brand system in native WPF: exact tapered green version identifier, licensed Anta/Cutive Mono/Inter Tight font files, micro-grid, chamfered data frames, animated runtime signal history, breathing random-acid dot matrix, and the two-tier live generation status panel.
- Fixed resumable installer downloads at the exact end-of-file boundary: completed `.partial` runtime and Wan files are now checksum-verified and promoted, while stale HTTP ranges are safely restarted after a 416 response.
- Added the standalone scaled-FP8 Wan 2.2 I2V loader, safe UMT5 FP8 prompt path, high/low expert switching, block-level CPU offload, float32 VAE decode, and native MP4 export without ComfyUI.
- Validated real two-expert I2V generation on an RTX 4070 with finite latents, finite non-black frames, live progress callbacks, and a decodable MP4.
- Added the scheduler, tokenizer, and architecture support files to the model pack and pinned Desktop setup to immutable Hugging Face revision `493b7c8ff0a451b6b4c049afb3e6396dbfa1c688`.

- Defined CineForge Desktop and CineForge Online as separate releases sharing one responsive interface and project contract but using different inference and privacy boundaries.
- Established CineForge Online as an SFW-only service with layered moderation for prompts, reference uploads, and generated video before delivery.
- Defined CineForge Online as a lightweight client plus CPU/API broker that uses third-party video-generation APIs; users require no CUDA, WebGPU, dedicated GPU, or local model download, and CineForge operates no generation GPU.
- Established bring-your-own-provider billing for CineForge Online: application access is free, users fund generation directly through their connected provider, and every paid request requires a visible cost estimate and confirmation.
- Removed the remaining optional Diffusers safety-checker hook from Desktop inference so the local edition performs no application-level prompt or output moderation.
- Identified the installable Windows product as **CineForge Desktop** across setup, Installed Apps, shortcuts, application metadata, release filenames, and documentation.
- Made the automatic four-component Wan download explicit in the desktop installer and public installation instructions.
- Narrowed CineForge to a dedicated Wan video-generation application.
- Removed LTX and built-in still-image generation from the supported product scope.
- Defined source-image/keyframe import as the entry point for every generation.
- Established separate GitHub application and Hugging Face model distribution tracks.
- Added mandatory release, provenance, compatibility, verification, and decision records.
- Created the public `TheBaldDudeCo/CineForge-Wan-Models` Hugging Face repository with model card, provenance, checksums, manifest, and validation state.
- Reworked Windows setup to ask for separate application and CineForge Library destinations.
- Added automatic, resumable Wan model downloads with aggregate progress, free-space checks, SHA-256 verification, retries, and safe failure recovery.
- Published the complete Wan core pack and pinned installer downloads to immutable Hugging Face revision `3abefe070febb87cf51e038edda29934743639fb`.
- Added an independently reproducible, version-pinned CUDA packaging environment for public Windows builds.
- Isolated CineForge inputs, outputs, projects, models, cache, logs, and temporary files from Shadowframe and removed Shadowframe model discovery paths.
- Recorded the verified CineForge Desktop 0.3.1 preview artifact, automatic model-delivery sequence, immutable model revision, checksum, and remaining public-release gates.

### Remaining release gate

- Clean-machine installation, automatic model download, launch, generation, and uninstall verification.

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

[Unreleased]: https://github.com/thebalddudeco/CineForge/compare/v0.5.0...HEAD
[0.5.0]: https://github.com/thebalddudeco/CineForge/compare/v0.4.0...v0.5.0
[0.2.0]: https://github.com/thebalddudeco/CineForge/releases/tag/v0.2.0
[0.1.0]: https://github.com/thebalddudeco/CineForge/releases/tag/v0.1.0
