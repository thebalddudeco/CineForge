# Installing CineForge on Windows

## Requirements

- Windows 11 x64
- Supported NVIDIA GPU and driver
- At least 42 GB free for the initial Wan pack, plus working space for generated video
- Reliable internet connection for the first model download

## Setup

1. Download the CineForge installer and `SHA256SUMS.txt` from the matching GitHub Release.
2. Verify the installer checksum.
3. Run setup.
4. Choose the **application folder**. The final folder must be named `CineForge`.
5. Choose the **CineForge Library** location. The final folder must be named `CineForge Library`.
6. Setup installs the application, creates the library, and automatically downloads the required Wan components.
7. Leave setup open until every component passes SHA-256 verification.

Downloads resume from `.partial` files if the network fails, setup closes, or the user pauses installation. Rerunning the installer with the same library path continues the transfer.

## Folder layout

```text
CineForge Library/
  inputs/
  outputs/
  projects/
  models/
    CineForge-Wan-2.2-I2V-A14B-FP8/
      components/
  cache/
  logs/
  temp/
  config.json
```

CineForge does not use Shadowframe's models, inputs, outputs, cache, projects, or application directory.

## Uninstall behavior

Uninstall removes the CineForge application and shortcuts. It preserves the entire CineForge Library, including models and creative work. The installer refuses to replace or remove an application folder that does not contain its CineForge ownership marker.

## Model revision

Version 0.3.0 downloads the verified model pack from immutable Hugging Face revision `3abefe070febb87cf51e038edda29934743639fb`. The installer will not follow later changes made to the repository's `main` branch.
