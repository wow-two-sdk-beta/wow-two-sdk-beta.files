# T5 — Identity: what File Storage needs

*Last updated: 2026-10-26*

> Yes, File Storage uses our Identity: it is an API resource that validates Identity tokens offline. This track lists
> what Files needs from Identity, what Files builds itself, and the asks to hand the Identity chat so its library and
> server ship them.

- Modes `M8`, `M10`, `M11` and questions `Q…` are the identity analysis (`wow-two-ws/ideas/identity-analysis-2026-10/identity-analysis-2026-10.md`)'s.

---

## 1 · How Files uses Identity

| Who calls | How | Identity mode | Phase |
|---|---|---|---|
| a product backend | client-credentials token, audience `files` | M10 | P1 |
| a browser, upload or download | a ticket or signed URL its product backend minted | none | P1 |
| a browser holding a user token (SPA, mobile) | user access token with audience `files` | M8 | P2 |
| a product acting for a person | token exchange: user token → `files` token with `act` | M11 | P2 |
| the Files console | OIDC sign-in, BFF | M2 | P1 |
| a share page with a sign-in gate | OIDC sign-in against the share owner's issuer | M2 | P2 |
| a B tenant without Identity | tenant API key | M0 primitives | P1 |

- P1 needs only M10 and the console's M2: the backend-minted ticket keeps user tokens out of P1.
- Files validates offline through JWKS (M8 logic), so an Identity outage costs no reads until tokens expire.

---

## 2 · Claims Files reads

| Claim | Use |
|---|---|
| `iss` | picks the tenant on B; must match the tenant's trusted issuer |
| `aud` | must contain the instance's `files` resource |
| `sub` | owner and audit actor; the only person key |
| `azp` / `client_id` | the calling app; per-app policy and quota |
| `org_id` | the active organization (identity Q8: one active organization) |
| `org_role`, `org_permission` | role and permission checks |
| `scope` | `files.read`, `files.write`, `files.share`, `files.admin`, `files.tenants` |
| `act` | the real caller under token exchange (P2) |

- these names land with the identity self-served item "claim vocabulary converges on `sub`, `sid`, `org_id`".

---

## 3 · Asks for the Identity chat

Ranked by when Files needs them.

| # | Ask | Why Files needs it | Identity side | When |
|---|---|---|---|---|
| I-1 | API resources with scopes: register `files` and its five scopes per instance | audience and scope checks | client and resource registry (Server) | Files P1 |
| I-2 | client credentials per app, with an `azp` claim and per-client scopes | product backends call Files | M10 grant (OpenIddict) | Files P1 |
| I-3 | stable claim vocabulary (`sub`, `org_id`, `org_role`, `org_permission`, `azp`) | policies key on it | converge before the first client ships | Files P1 |
| I-4 | multi-issuer validation: N trusted issuers, each with its own JWKS and audience, picked per tenant | B trusts Ocharo's and WoW2's instances at once | `Identity.Validation` today takes one `MetadataAddress` | Files P1 on B |
| I-5 | the API-key primitive usable without an identity store: hashed keys, prefix lookup, scopes | B tenants without Identity | `Auth` primitives (M0), leaving core with Identity (Q1c) | Files P1 on B |
| I-6 | OIDC client module for a BFF console | console sign-in | M2 client | Files P1 |
| I-7 | events as `IEvent` records in `Identity.Abstractions`: `MemberRemoved`, `OrganizationDeleted`, `PersonErased` | revoke the member's shares, purge an organization, erase a person's files | events in the contract (spec: webhooks P2) | Files P2 |
| I-8 | an avatar port, not a Files reference: Identity declares `IAvatarStore`, the host binds it to Files | profile avatars without a Files → Identity cycle | domain structure § Layers: a port in the domain's own contract | Identity P2 |
| I-9 | token exchange with `act` | user-scoped calls | M11 | Files P2 |
| I-10 | guest email proof: a person proves an email by code without membership | email-gated shares | guest sign-in by email code (Q21 guest upgrade) | Files P2 |
| I-11 | erasure fan-out lists Files as a target | personal-data erasure | Q21 | Files P2 |

- I-4 and I-5 are new to the identity analysis; the rest map to existing questions.
- Files never references Identity's implementation: its packages read the kernel `ICurrentUserService` only, and the
  `Files.Api` host composes `Identity.Validation` and `Identity.Client`.

---

## 4 · What Files builds itself

- share-link passwords: hashed with the core Argon2id hasher, attempt limits per share; never Identity accounts.
- share tokens, upload tickets, signed URLs: HMAC with instance keys rotated through the secrets store (D10).
- tenant registry on B: issuer, audience, API keys, hostnames, byte-store target, quotas.
- authorization policies: scope + organization + role + per-file visibility, in `Files.Module`.
- audit rows for every upload, delete, private download and share open, keyed by `sub` and `azp`.

---

## 5 · Dependency direction

- Files → Identity: contract and validation only, one way.
- Identity → Files: never; avatars go through the I-8 port.
- Files P1 does not wait on Identity P1: client credentials and API keys run on the be sdk handlers that exist today
  (`AddOAuth2ClientCredentials`, `AddApiKeyAuthentication`), and Ocharo's instance swaps in its Identity issuer once
  it is live.
