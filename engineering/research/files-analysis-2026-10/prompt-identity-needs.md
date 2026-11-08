# Prompt — new chat: what the file server needs from Identity

*Last updated: 2026-11-08*

> For a fresh chat started at the wow-two-ws root. Everything below the line is the prompt.

---

Settle what WoW2's file server (Files, `wow-two-sdk-beta.files`) needs from Identity before its build starts. Identity's
current scope closed on 2026-11-08, and the order is Identity, then Files, then Events (developer, 2026-11-05). This chat
analyses; it builds nothing until the developer confirms the result.

**Done when**

- every contract Files needs from Identity has a verdict (provided, change, add or drop) and a phase;
- the developer has confirmed what Identity must add before Files P1;
- the confirmed picks are written into Files' analysis or spec and into Identity's backlog.

**Read first** (paths from the wow-two-ws root)

- Files' contracts, C1–C15 (2026-10-26):
  `workbench/wow-two-sdk-beta/wow-two-sdk-beta.files/engineering/research/files-analysis-2026-10/t8-identity-contracts.md`,
  with `t5-identity.md` (why) and `t1-services-tenancy.md` § 2 (instance kinds A and B) beside it.
- Files' spec: `workbench/wow-two-sdk-beta/wow-two-sdk-beta.files/product/product.md`.
- The settled rules, which supersede parts of t8: `conventions/development/backend/dotnet/core/hla/api-communication.md`.
- Identity as built, at `3c11365`: `workbench/wow-two-sdk-beta/wow-two-sdk-beta.identity`, starting with
  `product/features/service-tokens.md` (its "Differences from T8" table) and `product/features/revocation-signals.md`,
  then the packages under `engineering/codebase/identity.backend-services/packages`.

**Identity's state**

- Released 2026-11-06: image `v0.5.20`, NuGet `10.0.29-beta`, npm `0.0.15`. Ocharo's dev runs it; Ocharo prod is parked.

**Leads from a read-only check on 2026-11-08** (code reading only; verify before relying on them)

- Provided: C1 principal normalizer (the interface and `PrincipalKind` live in core), C3 resources with the audience
  from the registry, C4 client credentials with a secret or `private_key_jwt`, C6 token exchange with `act`, C8 trusted
  issuers (`JwtOptions.TrustedIssuers`, `ITrustedIssuerResolver`), C15 the issuer on the principal.
- Partly provided:
  - C2: `azp` became `client_id`, and the API-key principal carries no `iss`.
  - C7: no stated rule keeps a retired key in JWKS for its lifetime plus cache time, and the cache and refresh timings are unconfirmed.
  - C9: no `{partition}_{keyId}_{secret}` lookup; the repository finds keys by hash only (`FindLiveByHashAsync`).
  - C10: the BFF exists, but there's no `IOidcSchemeResolver` for per-tenant schemes.
  - C12: only the session-ended and access-changed signals exist. They are JWT-signed and go to one URL per resource,
    and there's no `person.disabled` or `person.erasure_requested`.
  - C14: onboarding is configuration only, with no admin APIs.
- Missing: C11 (`AllowGuests`, email-proof tokens) and C13 (`IAvatarStore`).
- Superseded by `api-communication.md`:
  - C5's delegation headers and `files.delegate` give way to token exchange.
  - A person's call needs a scope plus a role or `org_permission`, not a scope alone.
  - t8's Standard Webhooks events give way to the two signals, sent through `Messaging/Webhooks`.

**Settled; don't reopen** (developer, 2026-11-05)

- Identity issues audience-bound tokens that services verify offline. A service acting as itself uses client
  credentials, and one acting for a person uses token exchange.
- Each caller gets one token per target, listing its scopes, and scopes are named `{service}.{action}`.
- Clients prove themselves with a secret by default, or a key set (`private_key_jwt`) by configuration.
- Revocation runs through the two signed signals. Long work is authorized when accepted, then runs as the service.
- API keys are only for callers outside Identity, such as Files' outside tenants.

**How to work**

- SDK doctrine: Files' need triggers the vector, and Identity gets the whole vector, not only Files' slice (`CLAUDE.md`
  § SDK doctrine, `conventions/development/dev-cycle.md` § Vector completeness).
- Delegate reads across many files to agents. Keep decisions in the chat, one per turn.
- Out of scope: Events, Ocharo prod, Files' own implementation, and the Identity checks the developer parked
  (Ocharo `ocharo-platform` `engineering/planning/backlog.md`, row "Dev sign-in checks").

**Context**

- Earlier Files chat: `w2 - file server` (`local_292adfb3-8a44-47d6-9073-ebd751a9a2c9`), idle since 2026-11-03; its
  transcript holds the October analysis.
- Ocharo waits on Files. The `ocharo-platform` backlog row "Ocharo File Storage adoption" moves Catalogue, Brand, Blog and
  Marketing media, then Desk's `bytea` files.
- Identity's closing record is in `ocharo-platform` `engineering/planning/identity-cutover.md`, #52–#54.

**First reply**

- List the t8 contracts that block Files P1 today, each with the Identity change it needs.
- Then give the developer one decision.
