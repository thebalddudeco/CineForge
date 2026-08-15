# CineForge Visual Design Contract

Status: locked from the complete design conversation and approved preview assets. This document is the implementation authority for CineForge Desktop visual work. A later explicit user decision may supersede it; incidental prototypes may not.

## Product character

CineForge is a cinematic production instrument, not a conventional dark form application with decorative green accents. The interface should feel like a coherent retro-futurist monitoring and control system:

- black and carbon instrument surfaces;
- alabaster primary information;
- lower-opacity alabaster secondary telemetry;
- neon chartreuse and lime flash reserved for active, live, selected, connected, or progressing states;
- thin technical linework, clipped and tapered frames, corner brackets, rails, ticks, small system labels, data callouts, segmented indicators, and restrained grid backgrounds;
- visual density created through useful hierarchy and telemetry, not ornamental noise;
- no generic blue/purple cyberpunk palette;
- no gradients, glows, pills, rounded consumer-app cards, or unrelated sci-fi motifs unless explicitly approved later.

The visual system must remain legible and usable. Brand treatments never justify clipped text, low-contrast controls, distorted symbols, or unclear hierarchy.

## Color behavior

The supplied five-color CineForge palette is authoritative:

- Neon Chartreuse: `#E4FF1A`;
- Carbon Black: `#242424`;
- Black: `#020300`;
- Alabaster Grey: `#E0E0E0`;
- Lime Flash: `#89FC00`.

Neon Chartreuse is the primary active/action color. Lime Flash is the secondary live-signal color. They identify progress, active selections, connected status, percentages, scan cursors, migrating matrix cells, and primary actions. Alabaster Grey carries readable information on Carbon Black and Black.

Contrast is a non-negotiable component rule:

- Alabaster Grey text may appear only on Carbon Black or Black backgrounds.
- Black text must be used on Neon Chartreuse, Lime Flash, or Alabaster Grey backgrounds.
- Light text must never appear on a light background, including hover, focus, selected, pressed, disabled, and dropdown states.
- Muted text and technical lines are created with opacity variants of Alabaster Grey rather than introducing unrelated gray colors.

## Typography

Fonts ship with the application and their licenses. The installed desktop app must not fetch fonts from the web.

### Latin

- Anta: CineForge identity, hero titles, and major page titles.
- Saira Condensed: panel headings, navigation, buttons, model names, and operational controls.
- Cutive Mono: shot numbers, versions, percentages, timestamps, seeds, runtime status, job identifiers, and technical metadata.
- Inter Tight: prompts, forms, descriptions, cards, menus, tooltips, and longer body copy.
- Kumar One Outline: rare display accent for launch/empty/promotional moments; never the everyday interface title font.

### Korean

- Gugi: title.
- Orbit: subheads and controls.
- IBM Plex Sans KR: body copy.

### Japanese

- M PLUS 1 Black 900: title.
- Zen Kurenaido: subheads and controls.
- Zen Kaku Gothic Antique: body copy.

### Language selection

- Setup presents English, Korean, and Japanese before installation and saves the selection into the CineForge install metadata and library configuration.
- CineForge Desktop exposes three compact controls at the bottom-left: `EN`, `한`, and `日`.
- The active language control uses Neon Chartreuse with Black text; inactive controls use Carbon Black with Alabaster Grey text.
- Switching language updates interface copy and the complete title/subhead/body font set immediately without restarting.
- An in-app choice is stored as the user preference and takes precedence over the original installer selection on later launches.
- All translation dictionaries and font files ship in the desktop runtime; localization remains available offline.

## Exact version identifier

The compact approved preview is `design-previews/cineforge-version-badges-brand-green-compact.png`.

The silhouette must not be improvised, shortened, widened, converted to a hexagon, converted to an inverted triangle, or stretched to fit a header slot. Its geometry is:

- horizontal flat top;
- short outward-sloping shoulders;
- widest points at the upper side corners;
- long inward-tapering sides;
- narrow horizontal flat bottom;
- vertically elongated overall proportion.

Two variants are permitted:

- primary/filled: acid-green silhouette with black Cutive Mono version text;
- secondary/outline: transparent/near-black interior, one-pixel acid-green silhouette, acid-green Cutive Mono version text.

The number is centered visually within the original silhouette. Scaling is uniform in both axes. The exact aspect ratio is preserved. The badge may be smaller in the application header than in a design sheet, but it may never be squashed.

Earlier hexagonal and triangular explorations are superseded by this exact tapered badge.

## Generation monitor

The approved source is `design-previews/cineforge-generation-window-dot-matrix-still.png` and its animated study. The production control is native and driven by real job data; the GIF is a design reference only.

Required structure:

- double technical outer frame with clipped corners, corner fasteners/details, fine interior rails, and a small lower-left hatch detail;
- top status line with `GENERATING`, `FRAME SYNTHESIS`, real percentage, and real job identifier;
- fixed rectangular field of small square matrix cells;
- segmented progress rail beneath the matrix;
- current phase plus real step/frame information;
- lower telemetry bay with local runtime status, elapsed time, ETA, and signal-lock segments;
- restrained footer/system identifiers such as CineForge/local system and DMX/live equivalents.

Required matrix behavior:

- every cell follows the same slow breathing duration but receives an asynchronous phase;
- opacity traverses the full intended dim-to-bright range without the entire field blinking in unison;
- a small, changing subset of cells uses acid green;
- green positions migrate over time and do not remain assigned to fixed cells;
- the matrix remains within a fixed rectangular boundary and does not rearrange its grid;
- progress, elapsed time, ETA, percentage, phase, frame, and step values come from the active generation job;
- the production monitor is not a GIF or video playback.

The matrix is the approved primary generation visual. Orbital, radar, and segmented signal-core graphics remain approved secondary motion vocabulary for planning, acquisition, model loading, encoding, and other distinct phases; they do not replace the live matrix monitor.

## Runtime connection monitor

The approved source is `design-previews/cineforge-runtime-radio-history-still.png` and its animated study. The production control is native and reflects real runtime data.

Required structure and behavior:

- clipped-corner instrument frame with a quiet grid;
- `RUNTIME SIGNAL HISTORY` heading and top-right `CONNECTED / 01` state;
- four horizontally flowing waveform histories;
- first waveform and moving scan cursor in acid green;
- remaining waveforms in ivory/gray;
- right telemetry column showing GPU, VRAM, engine, and build;
- values originate from the connected local runtime, not placeholder copy;
- animation is subtle and continuous, resembling signal history rather than an audio visualizer.

This monitor replaces a flat one-line connected-device strip where space permits.

## Frames, borders, and data callouts

The approved concept board is `design-previews/cineforge-retrofuture-concept-board.png`.

Its language must recur throughout the product:

- clipped-corner and tapered panel outlines;
- nested one-pixel frames for high-importance modules;
- corner brackets around focal regions;
- long title rails ending in a small circle or terminal marker;
- small acid square preceding system headings;
- calibration ticks, scan marks, short hatch groups, node dots, divided telemetry bays, and compact coordinate/job/build labels;
- segmented rather than generic continuous indicators where appropriate;
- clear separation between identity, controls, content, and live telemetry.

These motifs must be implemented as reusable native components/styles, so the app has one visual grammar instead of unrelated one-off decorations.

## Application-level mapping

- Header: Anta identity, system subtitle, live engine state, exact compact tapered version badge, technical rail/detail work.
- Workflow navigation: Saira Condensed operational labels, numbered states, acid active state, technical dividers.
- Forms: Inter Tight input text, Saira Condensed labels/actions, Cutive Mono metadata; clear contrast and comfortable spacing.
- Canonical reference: a framed input module with a fully legible primary action and visible selected-reference state/preview.
- Shot factory: structured branch controls and shot cards using the same clipped frames and telemetry hierarchy.
- Runtime area: approved runtime signal-history monitor.
- Generation: approved framed live dot-matrix monitor.
- Footer/status: quiet Cutive Mono build/runtime/system information.

## Superseded or rejected treatments

- A generic hexagon version badge.
- An upside-down triangle version badge.
- A short, wide, or squashed version badge.
- A badge merely inspired by the reference instead of matching the approved tapered silhouette.
- Ivory-filled badge in the application; the filled version uses the brand acid green.
- Fixed green dots in the matrix.
- A synchronized blinking matrix.
- A prerecorded GIF used as the real generation progress monitor.
- A flat connected-hardware sentence as the final runtime treatment.
- Blue/purple cyberpunk color substitutions.
- A browser/localhost window as CineForge Desktop's application shell.

## Current native-build audit

The native WPF conversion establishes the correct architecture and includes parts of the palette, grid, generation panel, matrix, runtime graph, and base Latin fonts. It is not yet visually complete against this contract:

- the exact badge must be implemented as a reusable aspect-locked component rather than an ad hoc header polygon;
- Kumar One Outline is bundled but still needs an approved special-display placement before it appears in the production interface;
- the runtime monitor is compressed into the sidebar and omits the approved four-band layout and full telemetry column;
- the generation monitor approximates the approved frame but does not yet reproduce its complete rail, corner, telemetry, and segmented-progress system;
- technical frame/callout vocabulary is not consistently componentized across forms, navigation, shot cards, and reference selection;
- layout and contrast need visual QA at supported window sizes so branding never clips or obscures controls.

No native interface pass should be called design-complete until it is visually compared at full size with the four authoritative previews named above.
