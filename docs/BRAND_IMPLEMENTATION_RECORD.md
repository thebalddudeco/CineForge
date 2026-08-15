# CineForge Desktop brand implementation record

Status: implementation paper trail for the v0.5.0 native Windows interface. The normative rules remain in `DESIGN_SYSTEM.md`; the archived structural reference remains in `V010_VISUAL_SOURCE_OF_TRUTH.md`.

## Design lineage

The production interface builds on the v0.1.0 composition and the approved futuristic micrographic references. It is not a generic dark application with green highlights and it is not a decorative HUD overlay. The intended character is a restrained cinematic instrument in which technical graphics communicate actual runtime, model, workflow, continuity, and generation state.

## Locked palette and contrast

| Role | Value | Production use |
| --- | --- | --- |
| Neon Chartreuse | `#E4FF1A` | primary action, selected state, progress, filled version badge |
| Carbon Black | `#242424` | raised instrument surfaces and inactive controls |
| Black | `#020300` | application field and dark input surfaces |
| Alabaster Grey | `#E0E0E0` | primary text and ivory technical lines |
| Lime Flash | `#89FC00` | secondary signal, connected state, migrating matrix cells |

Contrast is component state, not a suggestion:

- chartreuse, lime, and alabaster surfaces use black text;
- black and carbon surfaces use alabaster text;
- hover, focus, selected, pressed, disabled, and dropdown states follow the same rule;
- generic Windows blue selection/focus styling is overridden by the CineForge palette.

## Production component mapping

| Approved decision | Native implementation |
| --- | --- |
| v0.1.0 composition and hierarchy | WPF hero, workflow rail, main instrument column, reference pack, and local model router |
| Clipped/tapered frames | `BevelChrome` and native path-based generation chrome |
| Sparse grid and stationary texture | `AtmosphereLayer` and `DitheredAtmosphereLayer`; grain remains static and 35% less prominent than the first native pass |
| Filled tapered six-sided version identifier | Header path with uniform aspect ratio, chartreuse fill, and black monospaced version text |
| Live runtime history | Native sampled GPU/VRAM traces and scan cursor; no prerecorded runtime GIF |
| Live generation status | Fixed 38 × 10 native matrix, asynchronous five-second opacity phases, migrating lime subset, separate progress bar, percentage, phase, elapsed time, ETA, and step data |
| Approved orbital globe | Exact user-supplied GIF embedded as `Assets/orbital-globe.gif`, decoded into complete native frames, displayed at 86% within its header viewport |
| Registration graphics | Inward-facing corner marks; the globe bay lower-right mark was corrected from a top-right path to a true bottom-right `┘` path |
| Multilingual typography | Bundled offline English, Korean, and Japanese dictionaries and approved fonts |

## Typography shipped with Desktop

- Latin: Anta, Saira Condensed, Cutive Mono, Inter Tight, and Kumar One Outline.
- Korean: Gugi, Orbit, and IBM Plex Sans KR.
- Japanese: M PLUS 1, Zen Kurenaido, and Zen Kaku Gothic Antique.

Font license files are packaged beside the font resources. The interface does not download fonts at runtime.

## Superseded visual concepts

The following explorations must not return unless explicitly approved in a later design decision:

- hexagonal, triangular, shortened, widened, or squashed version badges;
- white or alabaster text on chartreuse/lime controls;
- blue hover, focus, or dropdown selection states;
- moving or rearranging matrix cells;
- prerecorded generation-progress animations;
- animated full-page film grain;
- flat, unframed generic cards;
- indiscriminate sci-fi decoration that does not represent real state;
- procedural approximations of a user-supplied approved animation when the supplied asset is available.

## Review-driven corrections included

- restored v0.1.0 hierarchy instead of approximating it from screenshots;
- corrected panel corner topology and neighboring panel alignment;
- repaired clipped divider lines and removed unmatched connector tails;
- added space between the hero title and workflow map;
- unified the Motion node with the approved brand green;
- corrected all light-on-light control states;
- removed Windows blue selection color;
- completed Korean and Japanese interface localization coverage;
- reduced film grain intensity and added dithering for smoother gradients;
- used the exact supplied globe GIF, reduced its scale to prevent top/bottom clipping, and corrected its lower-right corner registration mark.
- moved the WAN model control into the scene generation settings beneath Clip Length and removed the redundant full-width model-router panel;
- moved the shot-factory action beneath the reference pack, required a locked reference before planning, and automatically brought generated candidates into view.

## Acceptance boundary

A visual patch is not considered complete merely because it compiles. It must be checked in the actual native application at representative window sizes and language settings. Release verification must confirm:

- no clipped text or animation;
- no incorrect corner orientation;
- no light text on light surfaces;
- no blue focus/selection state;
- no distorted version badge;
- no animated page grain;
- live telemetry and generation displays remain data-driven;
- all three languages switch completely and use the approved font families.
