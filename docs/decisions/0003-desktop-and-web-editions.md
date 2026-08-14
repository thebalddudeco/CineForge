# ADR 0003: Desktop and Web editions

- Status: Accepted
- Date: 2026-08-14

## Context

CineForge needs to serve two distinct environments: a private local workstation with downloaded Wan models and a lightweight experience available from mobile and desktop browsers. Treating both as one deployable runtime would either burden the web client with impossible local dependencies or weaken the Desktop edition's privacy and independence.

## Decision

CineForge will ship as two separately identified releases:

1. **CineForge Desktop**, a Windows application that downloads Wan models and performs unmoderated local generation on the user's hardware.
2. **CineForge Web Beta**, a responsive, free-to-access beta client backed by hosted inference and finite-capacity controls.

The editions share the visual system, project schema, generation request schema, progress events, and user workflow. They use separate runtime adapters, distribution pipelines, privacy statements, release notes, version labels, and security gates.

CineForge Desktop will not apply application-level prompt or output content moderation. Hosted-service controls required by law or infrastructure agreements remain isolated to Web Beta and are not inherited by Desktop.

## Consequences

- The current Windows installer and local engine remain explicitly branded `desktop`.
- Web Beta ships no Wan weights, CUDA runtime, or local Python service to the browser.
- The responsive frontend must obtain edition and capability data from the active backend.
- Local-only UI surfaces are hidden in Web Beta; hosted account, quota, and retention surfaces are hidden in Desktop.
- Project documents remain portable wherever their referenced media is available.
- Desktop and Web Beta maintain separate release notes and validation matrices.
