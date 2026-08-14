# Local runtime and model inventory

Inventory date: 2026-08-14

## Active runtime

- ComfyUI 0.30.0 at `http://127.0.0.1:8188`
- NVIDIA GeForce RTX 4070 with 12 GB physical VRAM
- PyTorch 2.10.0 + CUDA 13.0
- Bundled Python 3.12.11
- Runtime base: `A:\Shadowframe AI Local Distro`
- Runtime is loopback-only (`127.0.0.1`)

## Discovered model families

| Family | Primary use | Approximate discovered size | CineForge 0.1 status |
|---|---|---:|---|
| Anima | Still generation / image-to-image | 8.67 GB | Adapter available |
| Moody Real Mix | Photoreal still generation / image-to-image | 3.23 GB | Detected; active checkpoint has a `NextDiT` shape mismatch |
| RedCraft | Photoreal still generation / image-to-image | 12.24 GB | Adapter available |
| LTX 2.3 | Image-to-video | 33.91 GB | Detected; installed checkpoint is incompatible with the active runtime |
| Wan 2.2 | Image-to-video / text-to-video | 28.90 GB | Verified CineForge image-to-video adapter |
| Flux 2 | Still generation | 69.04 GB | Assets detected; adapter deferred |
| Qwen Image | Still generation | 20.85 GB | Assets detected; adapter deferred |
| Qwen Vision-Language | Vision encoding / caption support | 13.62 GB | Assets detected |

Sizes include matched supporting assets and may count shared or duplicated files where a family appears in more than one ComfyUI model category.

## Verified ComfyUI capabilities

- `UNETLoader`
- `CheckpointLoaderSimple`
- `WanImageToVideo`
- `LTXVConditioning`
- `LTXVImgToVideoInplace`
- `SaveImage`
- `SaveVideo`

## Routing policy

CineForge selects adapters, not arbitrary checkpoint filenames. A model button appears only when its required assets and runtime nodes are present. Version 0.1 uses RedCraft as the preferred photoreal proof renderer, falls back to another available still adapter, and uses Wan 2.2 as the verified motion path.
