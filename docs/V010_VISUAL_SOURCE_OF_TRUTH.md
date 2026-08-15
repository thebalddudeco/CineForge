# CineForge v0.1.0 Visual Source of Truth

Status: canonical visual authority for the CineForge native interface rebuild.

This manifest prevents the production interface from being redesigned from memory, an AI-generated approximation, or the later flat native prototype. Implementation must trace back to the sources below.

## Archived product source

- Public release archive: `release/CineForge-0.1.0-win-x64/`
- Original interface source commit: `34bc25c` (`Initialize CineForge with release paper trail`)
- Original micrographic interface stylesheet: `34bc25c:web/micro-ui.css`
- Original structural stylesheet: `34bc25c:web/styles.css`
- Original component hierarchy: `34bc25c:web/index.html`
- Original live telemetry and matrix behavior: `34bc25c:web/app.js`

The historical source is a visual and interaction reference only. CineForge Desktop remains a native WPF application and must not restore the browser/localhost architecture.

## Canonical visual assets

### Overall visual language

- `design-previews/cineforge-retrofuture-concept-board.png`

Authority for:

- micrographic density;
- technical rails and terminal circles;
- corner brackets;
- calibration ticks and hatch marks;
- orbital/radar geometry;
- nested clipped frames;
- small system identifiers and telemetry labels;
- restrained use of the live signal color.

### Generation monitor

- `design-previews/cineforge-generation-window-dot-matrix-still.png`
- `design-previews/cineforge-generation-window-dot-matrix.gif`
- `design-previews/cineforge-dot-matrix-breathe-green-v2-still.png`
- `design-previews/cineforge-dot-matrix-breathe-green-v2.gif`

Authority for the fixed square-cell matrix, asynchronous breathing, migrating green cells, segmented progress, percentage, elapsed time, ETA, frame/step data, signal lock, and the complete nested generation frame.

The GIFs demonstrate motion only. Production progress is rendered natively from live generation data.

### Runtime monitor

- `design-previews/cineforge-runtime-radio-history-still.png`
- `design-previews/cineforge-runtime-radio-history.gif`

Authority for the four-band signal history, green leading waveform, moving scan line, clipped instrument frame, GPU/VRAM/engine/build telemetry, and connected state.

### Secondary motion vocabulary

- `design-previews/cineforge-orbital-loader.gif`
- `design-previews/cineforge-radar-loader.gif`
- `design-previews/cineforge-signal-core-loader.gif`
- `design-previews/cineforge-particle-signal-loader.gif`
- `design-previews/cineforge-motion-preview-sheet.png`

These elements may identify distinct planning, acquisition, inference, encoding, or completion phases. They may not replace the live dot-matrix generation monitor.

### Exact version identifier

- `design-previews/cineforge-version-badges-brand-green-compact.png`
- `design-previews/cineforge-version-badges-reference-exact-compact.png`

The approved production silhouette is the compact vertically tapered badge: flat top, short shoulders, widest upper corners, long inward sides, and narrow flat bottom. The later hexagonal concept-board badge is not the production version identifier.

## Current palette override

The v0.1.0 visual structure remains authoritative, but its former color values are superseded by the user-approved five-color palette:

- Neon Chartreuse: `#E4FF1A`
- Carbon Black: `#242424`
- Black: `#020300`
- Alabaster Grey: `#E0E0E0`
- Lime Flash: `#89FC00`

Black is the dominant surface. Carbon Black is a contained secondary instrument surface. Alabaster Grey is used for readable content and technical lines on dark surfaces. Neon Chartreuse is the primary active/action signal. Lime Flash is the secondary live/connected signal.

Light text never appears on a light background. Black text is mandatory on Neon Chartreuse, Lime Flash, and Alabaster Grey fills.

## Approved typography override

The v0.1.0 hierarchy remains authoritative, while the approved bundled font families replace its original web fonts:

- English: Anta / Saira Condensed / Cutive Mono / Inter Tight
- Korean: Gugi / Orbit / IBM Plex Sans KR
- Japanese: M PLUS 1 Black 900 / Zen Kurenaido / Zen Kaku Gothic Antique

## Implementation rule

Every production component must identify its canonical source before implementation:

- application shell and hierarchy → archived v0.1.0 source;
- frames, rails, brackets, ticks, nodes, and microcopy → concept board;
- version badge → exact compact tapered reference;
- runtime telemetry → runtime signal-history reference;
- generation telemetry → framed dot-matrix generation reference;
- motion phase graphics → approved motion studies;
- palette and contrast → current five-color override;
- localized typography → approved language font sets.

No component is approved merely because it is black, green, futuristic, or technically functional.

## Explicitly excluded

- The current flat gray native form layout.
- Large undifferentiated Carbon Black slabs.
- Full-width Neon Chartreuse buttons used as layout filler.
- Sparse headers with a detached oversized version badge.
- Generic rectangular panels without technical framing.
- Generic dashboard styling.
- Hexagonal or triangular substitutions for the exact version badge.
- GIF/video playback as live runtime or generation telemetry.
- Reintroducing an HTML/browser/localhost application shell.
