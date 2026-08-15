# CineForge Desktop version history

This record connects the public repository history, design evolution, runtime architecture, installer work, and release-state decisions. It supplements `CHANGELOG.md`; it does not replace tagged release notes or verification evidence.

## Version-state rule

The executable, installer, engine protocol, version badge, packaging defaults, tag, release title, checksums, and compatibility record must agree before a release is published.

At the time of this record:

- GitHub contains the earlier `v0.4.0` prerelease tag;
- the checked-in Desktop application, engine, installer, packaging defaults, localized titles, telemetry, and badge report `0.5.0`;
- the native WPF and brand redesign are the v0.5.0 release-candidate work recorded in `Unreleased`;
- a `v0.5.0` tag and public release must not be created until every release gate passes and the release artifacts/checksums are produced.

## v0.1.0 — cinematic workflow prototype

The first CineForge build established the reusable FLORA-inspired cinematic topology:

1. canonical/reference material;
2. five angle prompts;
3. five insert prompts;
4. five story-progressing prompts;
5. continuity selection;
6. motion direction;
7. image-to-video generation.

The release also established the visual hierarchy later designated as the source of truth: expansive editorial hero, compact workflow rail, dark instrument panels, sparse grid, technical micro-labels, strong negative space, and restrained active-state color.

The execution layer still relied on the early browser/ComfyUI architecture and multi-model discovery. Those implementation choices were later superseded; the workflow and design hierarchy were retained.

## v0.2.0 — standalone runtime engineering preview

The second milestone separated CineForge generation from the ComfyUI service and introduced:

- a standalone Python/PyTorch runtime;
- direct NVIDIA GPU and VRAM detection;
- native job telemetry including stage, step, elapsed time, ETA, output, and errors;
- the first Windows x64 packaging path;
- runtime signal-history and dot-matrix motion studies;
- a clean-process shutdown model.

This was an engineering preview. The bundled inference diagnostic worked without ComfyUI, but the final split/scaled-FP8 Wan production adapter and final native interface were not yet complete.

## v0.3.0 and v0.3.1 — installer and model-delivery previews

The v0.3.x work narrowed the product to CineForge Desktop as a Wan-only video application and established the public distribution contract:

- separate application and CineForge Library destinations;
- automatic, resumable model download from `TheBaldDudeCo/CineForge-Wan-Models`;
- immutable Hugging Face revision pinning;
- file-size and SHA-256 validation;
- preserved `.partial` transfers and safe retry behavior;
- isolated inputs, outputs, projects, models, cache, logs, and temporary data;
- no reuse of Shadowframe folders or model locations;
- reproducible CUDA packaging and build-specific release checksums.

The 0.3.1 preview recorded the installer artifact and remaining clean-machine release gates.

## v0.4.0 — native Wan release candidate

The tagged `v0.4.0` milestone completed the standalone Wan direction:

- direct split Wan 2.2 I2V A14B scaled-FP8 loading;
- scaled-FP8 UMT5 prompt encoding;
- high/low expert switching;
- block-level CPU offload for 12 GB-class GPUs;
- float32 VAE decode and native MP4 export;
- live generation progress callbacks;
- real two-expert RTX 4070 validation with finite latents and non-black decodable frames;
- prerelease GitHub packaging automation;
- resumable-download recovery and build-specific checksum fixes immediately after the tag.

The v0.4.0 public architecture still contained browser-hosted interface files. The post-tag work below replaces that interface while retaining the validated Wan engine path.

## v0.5.0 release candidate — native WPF and brand-system rebuild

The current work replaces the Desktop presentation and process boundary without changing CineForge into a hosted web application:

- genuine WPF application window;
- no Chrome launch, localhost listener, HTTP UI, WebView, or ComfyUI process;
- private redirected-process transport to the bundled Wan worker;
- native workflow, reference, shot-factory, model, generation, and output controls;
- offline English, Korean, and Japanese UI resources;
- bundled approved fonts for each language;
- live GPU/VRAM telemetry and runtime history;
- live 38 × 10 generation matrix and real progress data;
- complete implementation of the approved retrofuturist brand contract;
- exact filled tapered version identifier;
- exact user-supplied orbital-globe animation packaged as an application resource;
- numerous contrast, spacing, dropdown, registration-mark, grain, gradient, and clipping corrections from visual review.

All product version surfaces are now coordinated at `0.5.0`. The work remains in `Unreleased` until the remaining release gates pass, the installer/runtime assets and checksums are produced, and the `v0.5.0` tag is created.

## Release evidence

- Architecture decisions: `docs/decisions/`
- Visual contract: `docs/DESIGN_SYSTEM.md`
- v0.1.0 visual authority: `docs/V010_VISUAL_SOURCE_OF_TRUTH.md`
- Current brand implementation record: `docs/BRAND_IMPLEMENTATION_RECORD.md`
- Model compatibility: `docs/MODEL_COMPATIBILITY.md`
- Installer instructions: `docs/INSTALLATION.md`
- Verification status: `docs/RELEASE_VERIFICATION.md`
- Required release process: `docs/RELEASE_PROCESS.md`
