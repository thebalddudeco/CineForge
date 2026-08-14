# ADR 0002: Isolated CineForge Library and installer-managed Wan delivery

- Status: Accepted
- Date: 2026-08-14

## Context

CineForge must install and run independently, without borrowing application, model, cache, input, output, or project directories from Shadowframe. Public users also need a practical way to acquire the required Wan weights without manually assembling a model pack.

## Decision

The Windows installer asks for two locations: an application folder ending in `CineForge` and a data folder ending in `CineForge Library`. The locations must be separate and cannot contain one another.

The library owns `inputs`, `outputs`, `projects`, `models`, `cache`, `logs`, and `temp`. The installed application reads the selected library from `%LOCALAPPDATA%\CineForge\install.json`; `CINEFORGE_DATA_ROOT` remains an explicit override.

Setup downloads the four core Wan components from `TheBaldDudeCo/CineForge-Wan-Models`, resumes partial transfers, checks available disk space, and verifies every SHA-256 digest before marking the pack complete. Public releases must pin an immutable Hugging Face revision.

Uninstall removes the application and shortcuts but preserves the CineForge Library.

## Consequences

- CineForge creative files and model assets cannot collide with Shadowframe data.
- Initial setup requires approximately 42 GB of free space and a reliable connection.
- Interrupted model downloads can resume without discarding completed bytes.
- A production installer cannot be released until the weight repository is complete and its immutable revision is pinned.
