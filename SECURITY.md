# Security policy

## Supported versions

CineForge is currently pre-alpha. Security fixes are applied to the latest development branch and the newest public release only.

## Reporting a vulnerability

Please use GitHub's private vulnerability reporting feature for this repository. Do not open a public issue containing exploit details, access tokens, private media, filesystem paths, or personal data.

Include the affected version, environment, reproduction steps, impact, and any safe proof of concept. Reports will be acknowledged as soon as practical.

## Local-processing boundary

CineForge is designed to run generation locally. Any future network operation—model download, update check, telemetry, crash reporting, or account integration—must be explicit, documented, and independently controllable. Secrets must never be committed to the repository or written to logs.
