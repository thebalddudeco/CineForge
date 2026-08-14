# CineForge product editions

CineForge has two planned releases. They share the same brand, responsive interface, project format, cinematic controls, progress language, and core workflow. They do not share the same inference or privacy boundary.

## Edition contract

| Capability | CineForge Desktop | CineForge Web Beta |
| --- | --- | --- |
| Primary devices | Windows desktop | Mobile and desktop browsers |
| Inference | User's local NVIDIA GPU | Hosted GPU workers |
| Model delivery | Automatic local Wan download | No model download to the client |
| Storage | User-selected CineForge Library | Hosted project and media storage with documented retention |
| Connectivity | Generation works locally after model installation | Internet connection required |
| Access model | Downloadable desktop release | Free-to-access beta with capacity controls |
| Interface | Full CineForge responsive studio | Same studio adapted to small screens and touch |
| Content layer | No CineForge prompt or output moderation | Service rules isolated to the hosted API boundary |

## Shared experience

Both editions retain:

- source-frame import;
- Wan image-to-video generation;
- motion, action, camera, seed, duration, resolution, and quality controls;
- dot-matrix generation monitor, framed status panel, live percentage, elapsed time, ETA, and generation phases;
- project save/load semantics and compatible project documents;
- CineForge typography, acid-green palette, micrographics, version identifier, responsive panels, and navigation;
- preview, history, and export workflows.

The user should not have to relearn CineForge when moving between editions.

## CineForge Desktop

Desktop is the private local workstation. The installer downloads the pinned Wan model pack into a user-selected CineForge Library. Generation does not call CineForge servers, ComfyUI, or Shadowframe. CineForge Desktop does not implement a prompt blacklist, an NSFW classifier, or an output moderation service. The user controls their local inputs and outputs and is responsible for lawful use, consent, privacy, likeness rights, and intellectual property.

Desktop-specific code owns local model installation, CUDA discovery, GPU telemetry, filesystem projects, native inference, and Windows packaging.

## CineForge Web Beta

Web Beta is a lightweight responsive client. It must not ship PyTorch, CUDA, Wan weights, or the local Python server to browsers. The client submits compatible project and generation requests to a versioned hosted API, receives queue/progress events, and displays outputs using the same CineForge progress and project vocabulary.

The free beta does not require a paid plan. Capacity protection may include accounts, per-user concurrency limits, queue limits, rate limits, output expiration, and clearly communicated availability. Those controls protect a finite hosted GPU pool and are not changes to the CineForge creative workflow.

Because Web Beta processes user material on hosted infrastructure, it must publish a privacy and retention policy before release. Any legally required or infrastructure-provider content controls belong at the hosted service boundary and must not be compiled into or silently imposed on CineForge Desktop.

## Shared technical boundary

The interface talks to an edition-neutral generation API:

- Desktop adapter: `http://127.0.0.1` local engine and local files.
- Web adapter: authenticated HTTPS API, object upload, hosted queue, progress stream, and signed output delivery.

Shared schemas cover projects, generation requests, progress events, runtime capabilities, errors, and outputs. Edition-specific operations—model installation, local path selection, hosted authentication, billing, quotas, and retention—remain outside those shared schemas.

## Web Beta release gates

- responsive touch-first verification on current mobile and desktop browsers;
- installable PWA shell and resilient reconnect behavior;
- hosted GPU queue with real progress, cancellation, retry, and failure recovery;
- secure direct uploads and signed output delivery;
- account, quota, rate-limit, and capacity messaging;
- privacy, retention, deletion, acceptable-use, and incident-response documentation;
- accessibility, performance-budget, security, and abuse-resistance testing;
- clear Beta labeling separate from CineForge Desktop versioning.
