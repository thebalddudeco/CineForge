# Installing CineForge Local

## Supported system

- Windows 10 or Windows 11, 64-bit.
- A supported NVIDIA GPU and current NVIDIA driver for local generation.
- A compatible standalone CineForge/Diffusers model pack.

## Install

1. Download `CineForge-Setup-0.2.0-win-x64.exe` and `SHA256SUMS.txt`.
2. Optionally verify the SHA-256 checksum.
3. Run the setup EXE and choose **Install CineForge**.
4. Launch CineForge from the installer, desktop shortcut, or Start Menu.

CineForge installs for the current Windows user. Projects and references are preserved during upgrades and uninstall. Model libraries and caches are configurable so large AI assets can be kept off `C:`.

## Generation runtime

PyTorch, Diffusers, Transformers, and CUDA support are bundled with CineForge 0.2.0. Python, Node.js, .NET, and ComfyUI are not required on the destination PC. Model weights are installed separately because individual packs can be many gigabytes and have separate licenses.

## Uninstall

Use **Settings → Apps → Installed apps → CineForge Local → Uninstall**. The uninstaller removes the application and shortcuts while preserving project and reference files.

## Windows SmartScreen

Unsigned prerelease builds may display a Windows SmartScreen warning. Production releases should be Authenticode-signed before upload. The included checksum verifies integrity but does not replace code signing.
