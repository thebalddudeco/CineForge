# Security policy

## Supported versions

CineForge is currently pre-alpha. Security fixes are applied to the latest development branch and the newest public release only.

## Reporting a vulnerability

Please use GitHub's private vulnerability reporting feature for this repository. Do not open a public issue containing exploit details, access tokens, private media, filesystem paths, or personal data.

Include the affected version, environment, reproduction steps, impact, and any safe proof of concept. Reports will be acknowledged as soon as practical.

## Local-processing boundary

CineForge Desktop is designed to run generation locally. It does not apply an application-level prompt blacklist, NSFW classifier, or output moderation service. Model download is the only required network operation in the current installation flow. Any update check, telemetry, crash reporting, or account integration must be explicit, documented, and independently controllable. Secrets must never be committed to the repository or written to logs.

CineForge Online has a different trust boundary because media and prompts are processed by a hosted service. It is SFW-only and must apply layered moderation to prompts, uploaded reference media, and generated outputs. The hosted edition must document retention, deletion, account, capacity, moderation, appeal, abuse-response, and provider-policy behavior before public testing. Desktop privacy claims must never be applied to CineForge Online.

CineForge Online provider credentials are user-owned secrets. They must be transmitted only over HTTPS, encrypted at rest when persistence is explicitly enabled, masked in the interface, excluded from logs and analytics, and removable at any time. CineForge must never embed its own billable provider credential in a public client or silently fall back to a CineForge-funded account.
