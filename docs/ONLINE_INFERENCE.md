# CineForge Online inference architecture

CineForge Online uses third-party video-generation APIs. CineForge does not own or operate a remote generation GPU, and users do not download the CineForge Desktop model pack.

## Runtime boundary

The browser/mobile client performs the interface, local project editing, source-media selection, upload, progress display, preview, and download. A small CineForge API broker performs authentication, rate limiting, SFW admission, temporary upload coordination, provider routing, queue normalization, webhook verification, SFW output inspection, and approved-result delivery.

The third-party provider owns the model weights and generation hardware. It receives only requests that pass CineForge Online admission checks.

## Provider adapter contract

Every provider adapter must implement:

- `submit`: map a normalized CineForge image-to-video request to a provider request;
- `status`: translate provider queue states into queued, preparing, generating, moderating, complete, or failed;
- `cancel`: request cancellation when the provider supports it;
- `result`: normalize video URL, dimensions, duration, frame rate, seed, and provider metadata;
- `verifyWebhook`: authenticate asynchronous provider callbacks;
- `estimateCost`: calculate the maximum provider cost before job admission;
- `delete`: remove temporary provider-hosted inputs or outputs when supported.

Provider identifiers and request IDs are internal. The public client receives CineForge job IDs so providers can be replaced without changing projects or the interface.

## Initial provider candidate

The initial candidate is fal's managed Wan image-to-video API because it exposes image-to-video endpoints, asynchronous queues, status/result retrieval, and webhooks. As of 2026-08-14, fal lists Wan 2.2 A14B image-to-video at $0.04 per output second for 480p, $0.06 for 580p, and $0.08 for 720p. Current pricing must be queried or re-verified before release and must not be hard-coded as a permanent product promise.

Official references:

- https://fal.ai/models/fal-ai/wan/v2.2-a14b/image-to-video
- https://fal.ai/models/fal-ai/wan/v2.7/image-to-video/api
- https://huggingface.co/docs/inference-providers/en/index

The adapter remains provider-neutral. Hugging Face Inference Providers or another compatible video API can be added later without changing the client contract.

## Free beta budget controls

Free user access does not make provider inference free to operate. At the currently listed Wan 2.2 rate, a five-second 720p generation is approximately $0.40 before moderation, transfer, storage, and application-backend costs. One thousand successful generations at that setting would therefore consume approximately $400 in provider inference.

The beta requires daily allowances, per-account concurrency limits, maximum duration/resolution, pre-admission cost estimates, a global spend ceiling, and an automatic pause when the budget ceiling is reached. Users must see capacity messages before uploading large media or entering a queue.

## Security requirements

- Never expose a provider API key in JavaScript, a mobile bundle, logs, or client-visible errors.
- Bind provider request IDs to authenticated CineForge job IDs.
- Verify every webhook signature and reject replays.
- Apply SFW admission before paying for generation and SFW output inspection before delivery.
- Use short-lived signed URLs and documented deletion/retention windows.
- Enforce server-side duration, resolution, file-type, file-size, and cost limits regardless of client input.
