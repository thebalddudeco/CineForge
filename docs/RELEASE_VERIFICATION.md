# CineForge Local 0.2.0 release verification

Verified on 2026-08-14 on Windows 11 with an NVIDIA GeForce RTX 4070 and ComfyUI stopped.

## Packaged runtime

- Backend: `native`
- Engine: `CineForge Engine 0.2.0`
- PyTorch: `2.10.0+cu130`
- Diffusers: `0.39.0`
- Transformers: `5.0.0`
- `inference_ready`: `true`
- ComfyUI URL: `null`

## Frozen EXE generation test

- Adapter: `cineforge-native-diagnostic`
- Seed: `43`
- Sampling progress: `8 / 8`
- Final status: `complete`
- Final phase: `OUTPUT SAVED`
- Output root: `X:\CineForge\data\generated`
- Process released successfully after `/api/shutdown`

The diagnostic pack verifies packaged model discovery, local weight loading, CUDA transfer, callback-driven step progress, decoding, media persistence, and clean shutdown. It is not shipped as a production creative model.

## Model compatibility boundary

The installed raw Anima, RedCraft, Moody Real, Qwen, LTX, and Wan files remain untouched. They are inventoried but marked `conversion required` when they use workflow-specific split or scaled-FP8 formats that have not yet passed the standalone adapter test.

## Distribution

The installer bundles the native inference libraries but not third-party model weights. It is currently unsigned and should be Authenticode-signed before a broad public release.
