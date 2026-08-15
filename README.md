# CineForge

CineForge is a Wan-powered cinematic video system with two planned editions. Both editions share the same visual language, project structure, and generation workflow.

- **CineForge Desktop** is the private, local-only Windows edition. It downloads the required Wan models and performs generation on the user's own compatible NVIDIA GPU.
- **CineForge Online** is the lightweight, responsive browser and mobile edition. It sends approved generation jobs through a secure CineForge API broker to third-party video-model APIs, so phones and laptops download no model weights and need no dedicated GPU. The application is planned to launch as a free-to-access beta; users connect and fund their own supported provider account for generation. CineForge does not subsidize provider inference.

The current repository and installer work are focused on CineForge Desktop.

## Project status

**CineForge Desktop 0.5.0 native-desktop release candidate.**

The application, engine protocol, installer, packaging defaults, localized title bars, runtime telemetry, and approved filled version badge are stamped consistently as `0.5.0`. The release remains a candidate until every verification gate in `docs/RELEASE_PROCESS.md` has passed.

The application now loads the split Wan 2.2 I2V A14B scaled-FP8 experts, scaled-FP8 UMT5 encoder, and Wan VAE directly through the CineForge Engine without ComfyUI. A real two-expert generation completed on an RTX 4070 with finite latents, finite non-black frames, live progress telemetry, and MP4 export. The remaining public-release gate is a clean-machine installer/download/generation/uninstall pass.

The installer is pinned to the validated release-candidate pack revision recorded in [docs/MODEL_PACK_SPEC.md](docs/MODEL_PACK_SPEC.md) and [docs/RELEASE_VERIFICATION.md](docs/RELEASE_VERIFICATION.md).

## How to use CineForge Desktop

For the complete workflow, control explanations, first-project example, and troubleshooting, read the **[CineForge Desktop 0.5.0 User Guide](docs/USER_GUIDE.md)**.

1. **Install CineForge Desktop.** Choose an application folder and a separate CineForge Library folder when prompted. Setup downloads and verifies the required Wan model pack automatically.
2. **Open CineForge.** Confirm that the header reports a connected GPU/runtime. If the model list is empty, use **Refresh Models** after setup has finished downloading the model pack.
3. **Describe the sequence.** Complete the Scene Brief fields, leave the validated **5 Seconds** profile selected, and select the installed Wan model from the model menu directly beneath Clip Length.
4. **Lock the visual reference.** In **Lock what must not drift**, select the source image that establishes the subject and visual continuity. The build action remains unavailable until a reference image has been selected.
5. **Build the 15-shot factory.** Select **Build 15-Shot Factory** beneath the reference pack. CineForge creates five angle prompts, five insert prompts, and five story-progressing prompts, then moves the view to the generated candidates.
6. **Review the candidates.** Compare the planned shots and choose the candidate that should become video.
7. **Generate the video.** Use **Generate Video** on the chosen candidate. CineForge sends that shot, the locked image, and its motion direction to the local Wan engine.
8. **Monitor generation.** The live generation instrument reports stage, percentage, elapsed time, estimated time remaining, GPU activity, and VRAM use. The segmented bar is real progress data; the breathing dot matrix is the active-generation signal.
9. **Open the result.** When generation completes, preview the clip or use **Open Output Folder** to access the exported file in the CineForge Library.

The language controls in the lower-left corner switch the interface between English, Korean, and Japanese. CineForge Desktop keeps imported media, project data, models, and generated outputs on the local machine.

> **v0.5.0 note:** the five reference cards currently share one canonical image, and the validated generation profile remains five seconds. The full user guide explains these behaviors and the planned workflow clearly.

## Product scope

- Wan-only local video generation
- Source-image/keyframe input
- Motion, action, and camera-direction prompting
- Duration, resolution, frame rate, quality, and seed controls
- Real-time stage, percentage, elapsed-time, ETA, and GPU/VRAM telemetry
- Local preview and export
- No ComfyUI service dependency
- No built-in still-image generator
- No LTX runtime
- No application-level prompt or output content moderation in CineForge Desktop

The Desktop edition is a local creative tool: prompts, imported media, and generated outputs stay under the user's control. Users remain responsible for complying with applicable law and respecting consent, privacy, likeness, and intellectual-property rights.

See [docs/PRODUCT_EDITIONS.md](docs/PRODUCT_EDITIONS.md) for the shared product contract and edition-specific runtime boundaries.

CineForge Online is an SFW-only hosted service. Prompts, uploaded reference media, and generated outputs must pass the layered moderation contract in [docs/ONLINE_MODERATION.md](docs/ONLINE_MODERATION.md).

## Repository map

- `desktop/CineForge.Desktop/` — native WPF Windows interface
- `cineforge/` — private Wan worker, model discovery, job orchestration, and project planning
- `packaging/` — Windows installer build and signing scripts
- `tests/` — automated checks
- `docs/` — architecture, compatibility, release, provenance, and decision records
- `.github/` — issue templates, pull-request template, and release automation

## Run CineForge Desktop from source

Desktop development currently requires Windows 11, Python 3.12, an NVIDIA CUDA-capable GPU, and a compatible CineForge Wan model pack. These hardware requirements do not apply to people using CineForge Online; Online generation compute is supplied by its configured third-party model provider.

```powershell
.\run.ps1
```

CineForge opens as a native Windows application. It does not open a browser, host a local website, bind a localhost port, or embed a web view. The WPF interface communicates with the bundled Wan engine over private redirected process streams.

## Verify

```powershell
python -m unittest discover -s tests -v
dotnet build desktop\CineForge.Desktop\CineForge.Desktop.csproj -c Release
```

## Releases and paper trail

Every user-facing release must include:

- a SemVer tag and immutable GitHub Release;
- a changelog entry;
- release notes with known limitations;
- installer SHA-256 checksums;
- model-pack compatibility and provenance records;
- verification evidence for supported hardware and workflows.

The required process is documented in [docs/RELEASE_PROCESS.md](docs/RELEASE_PROCESS.md). Architecture and scope decisions are recorded in [docs/decisions](docs/decisions).

The complete product and version history is recorded in [docs/VERSION_HISTORY.md](docs/VERSION_HISTORY.md). The locked visual rules and their current native implementation are recorded in [docs/DESIGN_SYSTEM.md](docs/DESIGN_SYSTEM.md), [docs/V010_VISUAL_SOURCE_OF_TRUTH.md](docs/V010_VISUAL_SOURCE_OF_TRUTH.md), and [docs/BRAND_IMPLEMENTATION_RECORD.md](docs/BRAND_IMPLEMENTATION_RECORD.md).

## Models

Model weights are distributed separately through [TheBaldDudeCo/CineForge-Wan-Models](https://huggingface.co/TheBaldDudeCo/CineForge-Wan-Models). The application repository never stores model weights. The CineForge Desktop installer asks for separate application and CineForge Library locations, then automatically downloads every required Wan component into the selected library with resumable transfers and SHA-256 verification. Users do not need to find or place model files manually. See [docs/MODEL_PROVENANCE.md](docs/MODEL_PROVENANCE.md) and [docs/MODEL_PACK_SPEC.md](docs/MODEL_PACK_SPEC.md).

The CineForge Library is isolated from every other application and contains `inputs`, `outputs`, `projects`, `models`, `cache`, `logs`, and `temp`. CineForge does not scan or write to Shadowframe directories.

Wan 2.2 is an upstream project by the Wan Team. CineForge is an independent application and is not affiliated with or endorsed by Alibaba or the Wan Team.

## License

CineForge application source is licensed under the [Apache License 2.0](LICENSE). Third-party models, libraries, fonts, and visual assets remain subject to their respective licenses.
