# CineForge Wan model-pack specification

A runnable pack is a self-contained, versioned directory consumable by the native CineForge Wan adapter.

## Required metadata

`cineforge-model.json` must record:

- schema version;
- pack identifier and semantic version;
- model family and upstream repository/revision;
- pipeline type (`image-to-video`);
- precision and quantization method;
- required components and relative file paths;
- SHA-256 for every distributed file;
- minimum CineForge version;
- tested operating system, GPU, VRAM, driver, and runtime versions;
- license and notice locations;
- conversion tool and exact reproducible command.

## Validation gates

1. Manifest schema and all checksums pass.
2. Pack loads without ComfyUI installed or running.
3. One deterministic smoke generation completes.
4. Progress stages, percentage, elapsed time, ETA, and cancellation state remain live.
5. Output video decodes and has the requested dimensions, frame count, and frame rate.
6. Peak VRAM and system RAM are documented.
7. A second launch uses the installed pack without downloading undeclared files.
8. Clean shutdown releases the GPU and leaves no worker process running.

## Publication states

- `experimental`: metadata complete; end-to-end validation incomplete.
- `candidate`: end-to-end generation passed on development hardware.
- `supported`: clean-machine installation and release verification passed.

Only `supported` packs appear as recommended in a stable CineForge release.
