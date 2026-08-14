# ADR 0001: Wan-only native runtime

- Status: Accepted
- Date: 2026-08-14

## Context

CineForge began as a general cinematic workflow prototype capable of routing still-image and video operations through an existing ComfyUI installation. The product direction was subsequently narrowed to a dedicated local video generator.

## Decision

CineForge will support Wan as its only public generation family. Users provide a source image or keyframe. CineForge directly loads and runs a compatible Wan pack without launching or contacting ComfyUI. LTX and built-in still-image generation are outside the supported product scope.

An internal adapter boundary remains so newer Wan versions and pack formats can be introduced without rewriting the UI or project format. That boundary is not a user-facing multi-model router.

## Consequences

- The UI, API, discovery, tests, installer, and documentation must remove still and LTX paths.
- Model distribution moves to a separate Hugging Face repository.
- The application repository and installer stay model-free.
- A production release is blocked until a real Wan generation passes the native validation gates.
