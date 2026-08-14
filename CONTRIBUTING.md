# Contributing to CineForge

Thank you for helping build CineForge. The current priority is a reliable, standalone Wan video pipeline on Windows.

## Before opening a change

1. Check existing issues and the roadmap.
2. Keep changes inside the Wan-only product scope.
3. Do not commit model weights, generated video, installers, secrets, private reference media, or licensed stock-design source files.
4. Add or update tests for behavior changes.
5. Add an entry under `Unreleased` in `CHANGELOG.md` for user-visible changes.
6. Add a decision record when a change alters architecture, distribution, compatibility, privacy, or licensing.

## Pull requests

Pull requests should explain what changed, why it changed, user impact, validation performed, and known limitations. Release-affecting changes must also update compatibility and release documentation.

## Model contributions

Model files are not accepted in this repository. Model-pack work must include upstream provenance, original and derivative licenses, conversion commands, exact checksums, file inventory, compatible application version, validation hardware, and reproducible generation evidence.

## Development checks

```powershell
python -m unittest discover -s tests -v
```

Never report a model as supported solely because discovery found matching filenames. Supported means the pack completed a real CineForge generation and passed the documented validation gates.
