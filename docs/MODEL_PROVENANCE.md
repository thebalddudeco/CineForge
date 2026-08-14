# Model provenance

## Supported family

CineForge targets Wan video generation only. The first adapter target is Wan 2.2 image-to-video A14B using separate high-noise and low-noise experts.

## Upstream

- Project: Wan 2.2
- Publisher: Wan Team / Wan-AI
- Official source: https://github.com/Wan-Video/Wan2.2
- Official model namespace: https://huggingface.co/Wan-AI
- License reported by the upstream project: Apache License 2.0

## Local development assets

The development workstation currently contains a workflow-oriented scaled-FP8 package composed of:

- `wan2.2_i2v_high_noise_14B_fp8_scaled.safetensors`
- `wan2.2_i2v_low_noise_14B_fp8_scaled.safetensors`
- `umt5_xxl_fp8_e4m3fn_scaled.safetensors`
- `wan_2.1_vae.safetensors`
- optional LightX2V four-step LoRAs

These filenames and their presence do not establish origin, conversion authorship, or redistribution rights for every derivative component. Before upload, each artifact must be matched to its download source and license. Files with unresolved provenance must not be published.

## CineForge policy

- Preserve upstream notices and licenses.
- Never imply that CineForge trained or owns Wan.
- Publish conversion methodology and hashes.
- Separate required Wan components from optional third-party acceleration LoRAs.
- Do not redistribute a derivative component until its specific source and license are verified.
- Pin released model packs to an immutable Hugging Face commit.
