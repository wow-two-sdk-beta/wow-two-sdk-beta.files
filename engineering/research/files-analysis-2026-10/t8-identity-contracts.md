# T8 — Identity contracts for Files

*Last updated: 2026-10-26*

> Every contract between Files and Identity, concrete enough for the Identity chat to build: the principal seam, the
> token and claim vocabulary, resources and scopes, service tokens, delegation, keys, multi-issuer validation, API
> keys, console and share sign-in, events, the avatar port and tenant onboarding. [T5](t5-identity.md) holds the why;
> this track holds the shapes. The ready-to-forward prompt is [prompt-identity-chat.md](prompt-identity-chat.md).

- Identity questions `Q…` and modes `M…` are the identity analysis (`wow-two-ws/ideas/identity-analysis-2026-10/identity-analysis-2026-10.md`)'s;
  `(U)` = unverified; shapes are proposals until the Identity chat accepts them.
- Owners: **core** = kernel `WoW2.Sdk.Beta.Be.Abstractions` in the core repo; **Identity** = the identity domain repo;
  **Files** = the files domain repo.

---

## Overview

| # | Contract | Owner | Files phase | Exists today |
|---|---|---|---|---|
| C1 | principal seam: who is calling | core | P1 | `ICurrentUserService` with `Guid? Id` and `UserKind` only |
| C2 | claim vocabulary | Identity | P1 | converging (identity self-served item) |
| C3 | API resources and scopes, audience = resource URI | Identity | P1 | client registry by config planned |
| C4 | client credentials per app | Identity | P1 | `AddOAuth2ClientCredentials` (caller side) |
| C5 | delegation headers in P1 | Files, scope from Identity | P1 | — |
| C6 | token exchange with `act` | Identity | P2 | OpenIddict supports the grant |
| C7 | discovery, JWKS, key rotation | Identity | P1 | I17, I18 planned |
| C8 | multi-issuer validation | Identity | P1 on B | `JwtOptions` takes one `Issuer`, `Audience`, `MetadataAddress` |
| C9 | API keys in the shared vocabulary | Identity | P1 on B | `IApiKeyRepository` by SHA-256 hash, scopes; no app, org or partition |
| C10 | console sign-in, per-tenant schemes | Identity | P1 operator, P2 tenants | OIDC client module planned |
| C11 | share sign-in gate, guest email proof | Identity | P2 | guest upgrade in Q21 |
| C12 | events and webhooks Files consumes | Identity | P2 | webhooks P2 in the identity spec |
| C13 | avatar port | Identity | Identity P2 | — |
| C14 | tenant onboarding on B | both | P1 | — |
| C15 | subject identity across issuers | Identity | P1 | D3: one person per instance |

---

## C1 · Principal seam (core)

The kernel seam carries a Guid id and a kind; a client-credentials token's `sub` is a client id, and Files needs the
organization, app, scopes and actor. Widen it, or add a sibling the same claims normalizer fills:

```csharp
public interface ICurrentPrincipal
{
    string? Subject { get; }              // sub; a person id, client id or API key id
    PrincipalKind Kind { get; }           // Anonymous · Guest · Person · Service · ApiKey
    string? Issuer { get; }               // iss; null for in-process Form 1
    string? ClientId { get; }             // azp / client_id: the calling app
    string? OrganizationId { get; }       // org_id: the one active organization (Q8)
    IReadOnlySet<string> Roles { get; }        // org_role
    IReadOnlySet<string> Permissions { get; }  // org_permission
    IReadOnlySet<string> Scopes { get; }       // scope
    ActorInfo? Actor { get; }             // act (C6) or delegation headers (C5)
    string? Partition { get; }            // the tenant a multi-issuer or API-key scheme stamped (C8, C9)
}
```

- BCL only; it qualifies for the kernel because the Files and Identity contracts both read it.
- `ICurrentUserService` stays for products that only need the person id.
- core releases first, then Identity and Files re-pin (domain structure § Core).

---

## C2 · Claim vocabulary

| Claim | Person token | Client-credentials token | API key principal | Files reads it for |
|---|---|---|---|---|
| `iss` | ✓ | ✓ | the instance itself | tenant on B (C8) |
| `aud` | the resource URI (C3) | the resource URI | — | rejects tokens for another instance |
| `sub` | person id | client id | key id | owner, audit |
| `azp` | the app | the client | the key's app | per-app policy and quota |
| `org_id` | active organization | when the client is bound to one | when the key is | data scope |
| `org_role` | ✓ (array) | — | — | role checks |
| `org_permission` | ✓ (array) | — | — | permission checks |
| `scope` | space-delimited | space-delimited | from the key | operation checks |
| `act` | under exchange (C6) | — | — | real caller in audit |
| `sid` | ✓ | — | — | back-channel logout |
| `jti`, `exp`, `iat`, `nbf` | ✓ | ✓ | — | replay window, lifetime |

- one normalizer maps every scheme — JWT, cookie session, API key — onto C1, so Files' policies never branch by
  scheme.

---

## C3 · Resources and scopes

- each Files instance is one API resource; its audience is the instance's base URL (RFC 8707 resource indicator), so a
  token minted for B is refused at an A.
- scopes:

| Scope | Grants |
|---|---|
| `files.read` | metadata, content, signed URLs |
| `files.write` | uploads, update, delete, references, jobs |
| `files.share` | create, change, revoke shares |
| `files.admin` | usage, audit, policies, imports in the caller's tenant |
| `files.delegate` | the C5 acting headers |
| `files.tenants` | B operator: tenants, keys, issuers, hostnames |

- P1 registration by configuration on every Identity instance that trusts a Files instance:

```json
"Identity": {
  "Resources": {
    "files-b": { "Uri": "https://files.example", "Scopes": [ "files.read", "files.write", "files.share", "files.admin", "files.delegate" ] }
  },
  "Clients": {
    "ocharo-desk": { "Grants": [ "client_credentials" ], "Resources": [ "files-b" ], "Scopes": [ "files.read", "files.write", "files.delegate" ] }
  }
}
```

- whether OpenIddict 7.7.2 issues `aud` from the `resource` parameter or from fixed client resources is to verify (U).

---

## C4 · Client credentials

- every product backend calling Files is a confidential client with its allowed resources and scopes (C3).
- token lifetime: 5–10 minutes; the caller caches per audience until expiry (the be sdk handler does).
- `sub` = `azp` = the client id; `org_id` only for a client bound to one organization.
- secret or `private_key_jwt` client authentication; secrets per the secrets convention (D10).

---

## C5 · Delegation in P1 (no token exchange)

Identity Q11 picks "the product checks the acting user in P1"; Files mirrors it:

- a client-credentials call may carry `Files-Acting-Subject` and `Files-Acting-Organization` headers.
- Files accepts them only with the `files.delegate` scope; otherwise it answers `403`.
- Files scopes the call's data to the asserted organization and writes both into the audit row.
- the product has already authorized the person; Files trusts the confidential client, never the browser.

## C6 · Token exchange (P2)

- RFC 8693: `grant_type=urn:ietf:params:oauth:grant-type:token-exchange`, `subject_token` = the person's token,
  `resource` = the Files URI, `scope` ⊆ the client's allowed scopes.
- the result keeps the person's `sub`, `org_id`, roles and permissions, and adds `act: { "sub": "<client id>" }`.
- allowed per client and resource pair; replaces C5 for that client.

---

## C7 · Discovery, JWKS, keys

- standard `/.well-known/openid-configuration` with `jwks_uri`; reachable from B's host, which sits outside the
  owner's network.
- every token carries `kid`; RS256 or ES256; never HS256 across services.
- rotation keeps the old key in JWKS for at least the access-token lifetime plus the validator's JWKS cache time.
- validators cache JWKS for up to 24 h and refresh once on an unknown `kid`, at most every 5 minutes.

---

## C8 · Multi-issuer validation

B trusts several Identity instances at once (WoW2's, Ocharo's); one `MetadataAddress` cannot express that.

```csharp
builder.Services.AddIdentityValidation(options => options.Resource = "https://files.example")
    .AddTrustedIssuers<FilesTenantIssuerResolver>();

public interface ITrustedIssuerResolver
{
    ValueTask<TrustedIssuer?> ResolveAsync(string issuer, CancellationToken cancellationToken);
}

public sealed record TrustedIssuer(string Issuer, Uri MetadataAddress, IReadOnlyList<string> Audiences, string? Partition);
```

- flow: read the unvalidated `iss` → resolve → that issuer's cached metadata and JWKS → validate signature,
  audience, lifetime → stamp `Partition` onto C1.
- an unknown issuer answers `401`; a resolved issuer's keys never validate another issuer's tokens.
- the single static issuer stays the default (an A); Files implements the resolver over its tenants table, so a new
  tenant needs no restart.

---

## C9 · API keys in the shared vocabulary

Today's primitive finds a live key by SHA-256 hash and emits a key id, name and scopes. B's tenants without Identity
need more:

- `ApiKeyRecord` gains `ClientId`, `OrganizationId`, `Partition`, `ExpiresAt`.
- the handler emits `sub` (key id), `azp`, `org_id`, `scope` and the partition in the C2 names, filling C1.
- key text carries a lookup prefix so B routes it before hashing: `{partition}_{keyId}_{secret}` (format open).
- Files implements `IApiKeyRepository` over its tenant keys; rotation runs two live keys per tenant.

---

## C10 · Console sign-in

- the Files console is a BFF client (M2, Q7): code + PKCE, back-channel logout, scopes `openid profile files.admin`
  (`files.tenants` for the operator).
- P1: on B only the operator signs in, through WoW2's Identity; on an A, the owner's admins through the owner's
  Identity.
- P2: a tenant's admins sign in on B through their own issuer → the OIDC client module picks the scheme at runtime:
  `IOidcSchemeResolver.ResolveAsync(tenant)` → authority, client id, secret.

## C11 · Share sign-in gate and guest email proof (P2)

- sign-in gate: the share page signs the viewer in through the share owner's issuer (C10's runtime scheme) and checks
  `org_id` or the allowed list.
- email gate: Identity's guest flow proves an address by code without membership; the ID token carries `email`,
  `email_verified=true` and no `org_id`; only clients flagged `AllowGuests` may request it.

---

## C12 · Events Files consumes (P2)

| Event | Payload | Files reacts |
|---|---|---|
| `identity.member.removed` | `org_id`, `sub`, `at` | revokes the member's shares; hands their files to the organization |
| `identity.organization.deleted` | `org_id`, `at` | soft-deletes the organization's files; purges after retention |
| `identity.person.disabled` | `sub`, `at` | suspends the person's shares |
| `identity.person.erasure_requested` | `sub`, `request_id` | erases personal files; reports completion |

- in-process (Form 1): `IEvent` records in `Identity.Abstractions` on the local bus.
- across services: Standard Webhooks (`webhook-id`, `webhook-timestamp`, `webhook-signature`), one secret per
  subscriber, retries with backoff.
- on B each tenant's Identity posts to `/api/files/v1/hooks/identity/{tenant}`.
- erasure completion: Files answers the fan-out target contract Identity defines for Q21 (shape open there).

## C13 · Avatar port

```csharp
public interface IAvatarStore   // in Identity.Abstractions
{
    Task<AvatarReference> SaveAsync(string subject, Stream image, string contentType, CancellationToken cancellationToken);
    ValueTask<Uri?> GetUrlAsync(string subject, int size, CancellationToken cancellationToken);
    Task DeleteAsync(string subject, CancellationToken cancellationToken);
}
```

- Identity ships a default over core `Storage/`; a host with Files binds an adapter shipped by Files, which references
  `Identity.Abstractions` only.
- Identity never references Files.

---

## C14 · Tenant onboarding on B

| Step | Identity instance (e.g. Ocharo's) | B |
|---|---|---|
| 1 | registers B's resource URI and scopes (C3) | creates tenant `ocharo` with hostnames and quota |
| 2 | registers each app as a client with its B scopes (C4) | adds the trusted issuer: `iss`, metadata URL, audience (C8) |
| 3 | adds B's identity webhook as a subscriber (C12, P2) | stores the webhook secret |
| 4 | — | issues interim API keys while the Identity instance is not live (C9) |

- P1 runs these by configuration; P2 by admin APIs, which Wheelhouse can drive.

## C15 · Subjects across issuers

- on B, `sub` alone is not unique: Files keys people as (`iss`, `sub`) in owners, audit and shares.
- Identity keeps `sub` stable and never reused within an instance (D3).
