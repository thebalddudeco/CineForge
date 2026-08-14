# ADR 0003: Desktop and Online editions

- Status: Accepted
- Date: 2026-08-14

## Context

CineForge needs to serve two distinct environments: a private local workstation with downloaded Wan models and a lightweight experience available from mobile and desktop browsers. Treating both as one deployable runtime would either burden the web client with impossible local dependencies or weaken the Desktop edition's privacy and independence.

## Decision

CineForge will ship as two separately identified releases:

1. **CineForge Desktop**, a Windows application that downloads Wan models and performs unmoderated local generation on the user's hardware.
2. **CineForge Online**, a responsive hosted client planned to launch as a free-to-access beta with finite-capacity controls and SFW-only moderation.

The editions share the visual system, project schema, generation request schema, progress events, and user workflow. They use separate runtime adapters, distribution pipelines, privacy statements, release notes, version labels, and security gates.

CineForge Online will not attempt browser-side or mobile-device Wan inference. Client devices require no CUDA, WebGPU, dedicated GPU, or local model pack. CineForge will operate a lightweight API broker but no generation GPU. A configured third-party video-model API performs prompt encoding, diffusion, decoding, and output assembly; the client is limited to the interface, media transfer, progress display, playback, and download.

CineForge Desktop will not apply application-level prompt or output content moderation. CineForge Online will apply defense-in-depth SFW moderation to prompt text, reference uploads, and generated outputs before delivery. Online controls remain isolated to the hosted service and are not inherited by Desktop.

## Consequences

- The current Windows installer and local engine remain explicitly branded `desktop`.
- CineForge Online ships no Wan weights, CUDA runtime, or local Python service to the browser.
- Provider credentials remain server-side and are never embedded in the web or mobile client.
- The responsive frontend must obtain edition and capability data from the active backend.
- Local-only UI surfaces are hidden in CineForge Online; hosted account, quota, moderation, and retention surfaces are hidden in Desktop.
- Project documents remain portable wherever their referenced media is available.
- Desktop and CineForge Online maintain separate release notes and validation matrices.
