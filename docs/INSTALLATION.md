# Installing CineForge Desktop on Windows

## Requirements

- Windows 11 x64
- Supported NVIDIA GPU and driver
- At least 42 GB free for the initial Wan pack, plus working space for generated video
- Reliable internet connection for the first model download

## Setup

1. Download the CineForge Desktop installer and `SHA256SUMS.txt` from the matching GitHub Release.
2. Verify the installer checksum.
3. Run setup.
4. Choose **English**, **Korean**, or **Japanese** as the initial interface language.
5. Choose the **application folder**. The final folder must be named `CineForge`.
6. Choose the **CineForge Library** location. The final folder must be named `CineForge Library`.
7. Setup installs CineForge Desktop, creates the library, and automatically downloads all four required Wan components. No manual model download or file placement is required.
8. Leave setup open until every component passes SHA-256 verification.

The language can be changed later without reinstalling by selecting `EN`, `한`, or `日` in the bottom-left corner of CineForge Desktop. The latest in-app selection is remembered for future launches.

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

Version 0.5.0 downloads the verified model pack and native runtime support files from immutable Hugging Face revision `493b7c8ff0a451b6b4c049afb3e6396dbfa1c688`. The installer will not follow later changes made to the repository's `main` branch. After installation, CineForge launches as a native Windows application; it does not open a browser or start a localhost web server.

The public setup EXE is a lightweight bootstrapper. It first downloads and verifies the approximately 2.0 GB CineForge native CUDA runtime from the matching immutable GitHub Release, then downloads and verifies the approximately 35.6 GB Wan model pack. Both downloads resume after interruption.
