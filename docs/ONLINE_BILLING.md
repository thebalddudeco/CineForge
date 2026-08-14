# CineForge Online user-funded generation

CineForge Online is free to access during beta. Third-party model inference is not included or subsidized. Users connect and fund their own supported provider account, and the provider charges the user for successful generation according to its current terms and pricing.

## Beta billing model

- Bring your own provider account or API credential.
- Purchase credits directly from the provider.
- CineForge charges no application access fee during beta.
- CineForge does not maintain stored generation credits or a shared provider balance.
- CineForge never falls back to an operator-funded provider credential.
- Disconnecting a provider removes the usable credential from CineForge.

## Required confirmation

Before a paid job is submitted, CineForge Online must display:

- provider and model;
- output duration and resolution;
- current provider billing unit;
- estimated provider charge;
- a warning when the provider price could not be refreshed;
- an explicit action such as `Generate — estimated $0.40`.

The ordinary `Generate` action remains disabled until admission moderation passes, pricing is available, and the user confirms the estimated provider charge.

## Credential handling

Provider credentials must never be embedded in the public client. The user sends a credential to the CineForge API broker over HTTPS. Session-only use is the default. If the user explicitly chooses to save a connection, the credential is encrypted with a managed secret-encryption key, associated with that user only, masked in every response, excluded from logs and analytics, and deletable immediately.

Provider credentials must not be placed in project documents, browser local storage, error messages, support bundles, URLs, source maps, or webhook payloads.

## Moderation and charges

SFW input moderation occurs before provider submission to avoid spending credits on a request CineForge already knows it cannot deliver. Generated output is moderated before release. A provider may still charge for a successfully generated output that CineForge subsequently blocks during output moderation. CineForge must disclose that possibility before confirmation; blocked media remains unavailable and follows the quarantine/deletion policy.

Provider-side failures, refunds, credit expiration, and billing disputes follow the provider's terms. CineForge records the estimate, provider request ID, final status, and any provider-reported charge metadata so users can reconcile activity without exposing their credential.

## Future payment options

Reselling CineForge credits or accepting payment inside CineForge is outside the beta scope. That would introduce payment processing, taxes, refunds, chargebacks, provider resale terms, and additional consumer disclosures and requires a separate decision record before implementation.
