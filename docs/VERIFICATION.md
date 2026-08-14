# Verification record

Verified on 2026-08-14 against the active local Shadowframe/ComfyUI runtime.

## Automated checks

- Python compilation completed without errors.
- Six unit tests passed: three branch-topology tests and three workflow-routing tests.
- Local API health and model discovery endpoints returned successfully.
- Browser QA passed at a 1280-pixel viewport with no horizontal overflow or console errors.
- The brief form created exactly 15 candidates: five angles, five inserts, and five story-progressing shots.
- Selecting a shot activated the motion gate and produced the expected motion prompt.

## Live model checks

### Still

- Adapter: RedCraft/Krea2
- Seed: `314159`
- Proof size: 512 × 288
- ComfyUI prompt ID: `11d86001-7056-49eb-83ea-00cd4cfa1bd2`
- Result: success in approximately 16 seconds
- Saved sample: `sample-output/verified-proof-frame.png`

### Motion

- Adapter: Wan 2.2 image-to-video
- Seed: `271828`
- Proof size: 512 × 288
- Frames: 17
- ComfyUI prompt ID: `714ef99e-2336-4e64-aaf8-60e0a9d5cd5a`
- Result: successful MP4 in under one minute
- Saved sample: `sample-output/verified-wan22-motion.mp4`

The motion check also verified CineForge's output-to-input promotion step: the RedCraft output frame was copied into ComfyUI's input library automatically before the Wan workflow was queued.

## Compatibility findings

- The installed Moody Real Mix checkpoint fails in the active runtime with a `NextDiT` shape mismatch. RedCraft is therefore the preferred still adapter.
- The installed LTX 2.3 GTAnimation checkpoint fails in the active runtime with model-shape and missing-audio-VAE errors. LTX is reported but disabled.
- Wan 2.2 is the verified motion adapter for this installation.
