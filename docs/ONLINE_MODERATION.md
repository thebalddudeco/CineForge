# CineForge Online SFW moderation contract

CineForge Online is an SFW-only hosted service. This contract applies only to CineForge Online. CineForge Desktop remains a local tool without CineForge prompt or output moderation.

No automated system is perfect. Online moderation therefore uses independent checks before generation, during job admission, and before any result is delivered. Ambiguous or unavailable moderation results fail closed.

## SFW baseline

CineForge Online blocks content that includes or meaningfully facilitates:

- nudity, pornography, explicit sexual activity, fetish content, or sexual solicitation;
- sexualized people who are or may be minors, with zero tolerance for uncertainty;
- graphic injury, gore, torture, or glorified real-world violence;
- hateful or extremist praise, recruitment, or dehumanizing attacks;
- non-consensual intimate imagery, sexual deepfakes, or exploitative likeness use;
- instructions intended to evade, disable, or probe the moderation system.

Ordinary cinematic action, non-graphic peril, romance without explicit sexual content, swimwear in an appropriate non-sexual context, and documentary or educational material may be permitted only when every automated stage classifies the request as SFW.

## Defense-in-depth pipeline

1. **Text admission:** Normalize the prompt and negative prompt, detect obfuscation, and classify the complete request before a GPU job is created.
2. **Reference admission:** Scan every uploaded image and representative frames from uploaded video before storing it as a usable project asset.
3. **Context decision:** Combine text and media signals so individually ambiguous inputs cannot bypass policy through multimodal context.
4. **Generation isolation:** Only admitted jobs are submitted to the third-party model API. The CineForge job record retains an immutable moderation decision identifier and provider request identifier.
5. **Output inspection:** Sample frames across the generated video, including scene-change and peak-motion frames, and classify the combined video before release.
6. **Delivery gate:** Do not issue a preview URL, thumbnail, download URL, or share link until output inspection passes.
7. **Abuse controls:** Apply rate limits, repeat-violation controls, audit events, and account action without exposing classifier thresholds.

## Failure behavior

- A blocked input is rejected before GPU capacity is consumed.
- A blocked output is quarantined and never delivered to the user.
- A timeout, classifier outage, corrupted upload, unsupported media type, or inconclusive decision is treated as not approved.
- The interface provides a short category-level explanation and a safe way to revise the request.
- Appeals and false-positive reports must not expose raw private media to staff unless the user explicitly submits it for review.

## Data minimization

Moderation records should retain decision identifiers, category codes, model/ruleset versions, timestamps, and necessary account-abuse signals. Raw prompts and media must follow the published Online retention schedule and must not be placed in ordinary application logs. Access to quarantined media must be restricted and audited.

## Release requirements

CineForge Online cannot enter public beta until:

- text, image, and sampled-video classifiers are integrated;
- obfuscated-prompt and multimodal bypass tests pass;
- fail-closed behavior is verified for every moderation dependency;
- previews, thumbnails, downloads, and shares are all behind the delivery gate;
- false-positive reporting, account enforcement, and incident response are operational;
- classifier and ruleset versions are recorded for reproducibility;
- privacy, retention, deletion, and acceptable-use documents are published;
- red-team tests cover sexual content, minors, gore, hate, non-consensual imagery, and evasion attempts.
