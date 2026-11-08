# T8 — Identity contracts for Files

*Last updated: 2026-11-08*

> Every contract between Files and Identity, as Files builds against it: the principal, the claims, resources and
> scopes, service tokens, token exchange, keys, multi-issuer validation, API keys, console and share sign-in, the two
> revocation signals, the avatar port and tenant onboarding. [T5](t5-identity.md) holds the why. Rewritten 2026-11-08
> against `wow-two-ws` `conventions/development/backend/dotnet/core/hla/api-communication.md` (settled 2026-11-05) and
> Identity as built at `3c11365` (`product/features/service-tokens.md`, `revocation-signals.md`); the picks are
> [files-integration](../../planning/files-integration.md) #8–#23 and #35–#37. The 2026-10-26 proposal sits in git at
> `e3a29ae`.

- Owners: **core** = `WoW2.Sdk.Beta.Be.Abstractions` and `WoW2.Sdk.Beta.Be` in the be sdk; **Identity** = the identity
  domain repo's packages; **Files** = this repo.
- Phases: P1 = the Ocharo slice, P2 = the rest of the vector, per the [spec](../../../product/product.md).

---

## Overview

| # | Contract | Owner | Files phase | Files package | State 2026-11-08 | files-integration |
|---|---|---|---|---|---|---|
| C1 | principal seam | core | P1 | Module, Files.Api | provided | #8 |
| C2 | claim vocabulary | Identity | P1 | Module | provided; Files drops `azp` and `files_tenant` | #9 |
| C3 | resources and scopes | Identity | P1 | Abstractions | provided | #10, #35 |
| C4 | client credentials | Identity | P1 | Client | provided | #11 |
| C5 | delegation headers | — | — | — | dropped for C6 | #12 |
| C6 | token exchange with `act` | Identity | P1 | Client, Module | provided | #13 |
| C7 | discovery, JWKS, rotation | Identity | P1 | none | provided for Files; rotation is Identity's and Wheelhouse's | #14 |
| C8 | multi-issuer validation | Identity | P1 config, P2 resolver | Files.Api | provided | #15 |
| C9 | API keys | Identity | P1 on B | Files.Api | provided; no key prefix | #16 |
| C10 | console sign-in | Identity | P1 per #29, P2 tenants | Files.Api, apps/web | waits on `files-integration` #29 | #17 |
| C11 | share sign-in gate, guest email proof | Identity | P2 | Server, Files.Api | Identity adds | #18 |
| C12 | revocation signals and lifecycle events | Identity | P1 receive, P2 reactions | Files.Api, Module | receive provided; 2 lifecycle events to add | #19, #37 |
| C13 | avatar port | Identity | Identity P2 | new package | Identity lifts its store port | #20 |
| C14 | tenant onboarding on B | both | P1 config, P2 API | Module, Files.Api | configuration only | #21 |
| C15 | subjects across issuers | core | P1 | Module | provided | #22 |
| — | policy shape | Files | P1 | Module, Files.Api | Files builds | #23, #36 |

---

## Packages and pins

| Package | Carries | Version | Referenced by |
|---|---|---|---|
| `WoW2.Sdk.Beta.Be.Abstractions` | `ICurrentPrincipal`, `PrincipalKind`, `ActorInfo` | be `10.0.103-beta` or later | every package |
| `WoW2.Sdk.Beta.Be` | `AddOAuth2ClientCredentials`, `AddOAuth2TokenExchange`, `Messaging/Webhooks` | the same pin | Client, Module, Server, Files.Api |
| `WoW2.Sdk.Beta.Be.Identity.Auth` | `AddJwtBearerAuthentication` with `TrustedIssuers`, `ITrustedIssuerResolver`, `AddCurrentPrincipal`, `AddApiKeyAuthentication` and `IApiKeyRepository`, `AddIdentitySignalReceiver`, `MapIdentitySignals`, `AddTokenDenyList`, `RequireLiveToken` | `10.0.29-beta` | Files.Api |
| `WoW2.Sdk.Beta.Be.Identity.Client` | `AddIdentityBff` | `10.0.29-beta` | Files.Api, with C10 |
| `WoW2.Sdk.Beta.Be.Identity.Abstractions` | `SessionEndedEvent`, `AccessChangedEvent` | `10.0.29-beta` | Module, with the P2 reactions |
| `WoW2.Sdk.Beta.Be.Identity.Testing` | `FakeTokenIssuer` | `10.0.29-beta` | tests |

- the be floor is `10.0.103-beta`: `ICurrentPrincipal` first ships in `10.0.99-beta`, and Identity's `Auth` builds on `10.0.103-beta`.
- references run one way: Identity's `Auth`, `Client` and `Testing` reference only `Identity.Abstractions` and the be
  sdk, never `Identity.Server` or `Identity.Module`, so Files may take them.
- the packages (Abstractions, Module, Client, Server) read the kernel `ICurrentPrincipal` only; the `Files.Api` host
  composes Identity's `Auth` and `Client`, and the tests take `Testing`. A Form 1 host brings its own principal.

---

## C1 · Principal seam

- core's `ICurrentPrincipal` answers `Subject`, `Kind` (anonymous, guest, person, service, API key), `Issuer`,
  `ClientId`, `OrganizationId`, `Roles`, `Permissions`, `Scopes`, `Actor` and `Partition`; Identity's
  `AddCurrentPrincipal()` fills it from every scheme.
- Files' caller service reads it instead of raw claims, and the caller model carries kind, issuer and actor beside the
  tenant, subject, organization and app.
- `Files.Api` calls `AddCurrentPrincipal()`; a Form 1 host registers its own.

## C2 · Claim vocabulary

| Claim | Person token | Service token | API key principal | Files reads it for |
|---|---|---|---|---|
| `iss` | yes | yes | none: the key is this instance's own | owner and audit key (C15) |
| `aud` | the resource URI | the resource URI | — | refuses another instance's or tenant's tokens |
| `sub` | person id | client id | key id | owner, audit |
| `client_id` | the app | the client | the key's app | per-app policy and audit |
| `org_id` | active organization | a bound client's | a bound key's | data scope |
| `org_role`, `org_permission` | yes, permissions kept to `files:` | — | — | the #23 policy |
| `scope` | every scope the app may use on Files | the same | the key's | the #23 policy |
| `act` | under exchange | — | — | the real caller in audit |
| `sid` | yes | — | — | revocation (C12) |
| `partition` | stamped by the trusted issuer | stamped by the trusted issuer | the key's | the tenant on B |

- `azp` gave way to `client_id`; the normalizer reads both, Files reads the principal only.
- the tenant comes from `partition`, which the issuer's registration stamps; a token's own claims never pick it.
- a key principal has no `iss`; Files keys it as this instance's own, so Identity stamps none.

## C3 · Resources and scopes

- each Identity that trusts a Files instance registers it under `Identity:Server:Resources`:

```jsonc
{
  "Name": "files",                          // the same in every environment; a tenant move changes only Uri
  "Uri": "https://files.{tenant domain}/",   // the tenant's own host, with its slash (Uri.AbsoluteUri)
  "Scopes": [ "files.read", "files.write", "files.share", "files.admin" ],
  "PermissionNamespace": "files",            // an exchanged token keeps only files:* permissions
  "SignalUrl": "https://files.{tenant domain}/signals/identity"
}
```

| Scope | Grants |
|---|---|
| `files.read` | metadata, content, signed URLs |
| `files.write` | uploads, update, delete, references, jobs |
| `files.share` | create, change, revoke shares |
| `files.admin` | usage, audit, policies, imports in the caller's tenant |
| `files.tenants` | B's operator: tenants, keys, issuers, hostnames; only in the operator's issuer |

- B's audience is each tenant's own `https://files.{domain}/` (`files-integration` #35): B checks it per issuer through
  `TrustedIssuer.Audiences`, and a move from B to A changes DNS only, never the Identity entry.
- a tenant without a hostname of its own uses B's default host as its audience.
- each client lists the scopes it uses; Identity puts every one of them in each token for Files.

## C4 · Client credentials

- a service acting as itself, such as a job with no person, uses client credentials; its token has scope only.
- `Files.Client` takes the token endpoint, client id, `ClientSecret` or `ClientPrivateKey`, and the resource URI; the
  be sdk handler caches the token per audience and renews it near expiry.
- Files scopes a service call to its tenant (`partition`); a client bound to an organization stays in it, and an unbound
  one names the organization per request.

## C5 · Delegation headers — dropped

- token exchange (C6) replaces the acting headers and the `files.delegate` scope (`files-integration` #12).

## C6 · Token exchange

- a service acting for a person exchanges the person's token for one with the resource URI as `aud`; the result keeps
  the person's `sub`, `org_id`, roles and `files:` permissions, and adds `act: { "sub": "<client id>" }`.
- `Files.Client` exchanges when the call carries a person and uses client credentials otherwise; work accepted for a
  person later runs as the service, with the person recorded, never with their token.
- the caller's Identity client lists `token_exchange` and `files` under `ExchangeResources`.
- moved to P1: it is the only way a person's call reaches Files.

## C7 · Discovery, JWKS, keys

- Identity's `Auth` discovers each trusted issuer under its `Issuer` and refreshes keys on an unknown `kid`; Files
  configures nothing beyond the issuer.
- B's host must reach each trusted issuer's discovery and JWKS addresses, which sit outside B's network.
- rotation timing (old key kept for token lifetime plus cache time) is Identity's and Wheelhouse's (`files-integration` #14).

## C8 · Multi-issuer validation

```csharp
services.AddJwtBearerAuthentication(o =>
{
    o.Audience = "https://files.example/";                     // B's default host
    o.TrustedIssuers.Add(new TrustedIssuer
    {
        Issuer = "https://id.ocharo.example/",
        Audiences = [ "https://files.ocharo.example/" ],
        Partition = "ocharo",                                  // the Files tenant id
    });
});
```

- P1: the trusted issuers come from the tenant rows in configuration (C14); a new tenant takes a restart.
- P2: `FilesTenantIssuerResolver` over the tenants table, through `UseTrustedIssuerResolver<T>()`, so a new tenant takes
  none.
- an unknown issuer answers `401`; one issuer's keys never validate another's tokens.
- an A runs the same code with one trusted issuer.

## C9 · API keys

- callers outside Identity only: B's tenants without an Identity instance.
- Identity's `AddApiKeyAuthentication` reads `IApiKeyRepository`; Files implements it over its tenant keys: P1 from the
  SHA-256 hashes in configuration, P2 from a keys table with a unique hash index.
- `ApiKeyRecord` carries `ClientId`, `OrganizationId`, `Partition` and `ExpiresAt`, emitted as `client_id`, `org_id`,
  `partition` and `scope`.
- no `{partition}_{keyId}_{secret}` prefix: one hash lookup finds the key and its tenant.
- rotation runs two live keys per tenant.

## C10 · Console sign-in

- the console signs in through Identity's BFF (`AddIdentityBff`): code with PKCE, scopes `openid profile files.admin`,
  `files.tenants` for the operator.
- P1 waits on `files-integration` #29, which issuers B trusts:

| | WoW2 runs an Identity in P1 | WoW2 runs none in P1 |
|---|---|---|
| operator | signs in through WoW2's Identity, with `files.tenants` | edits tenants, issuers and keys by configuration (C14) |
| WoW2 apps on B | client credentials from WoW2's Identity | tenant API keys (C9) |
| B's console | the operator; tenant admins in P2 | Ocharo's admins through Ocharo's Identity, one static scheme scoped to `ocharo` |

- P2: tenant admins sign in through their own issuer; this needs Identity's runtime scheme resolver
  (`IOidcSchemeResolver`).

## C11 · Share sign-in gate and guest email proof (P2)

- sign-in gate: the share page signs the viewer in through the share owner's issuer (C10's runtime scheme) and checks
  `org_id` or the allowed list.
- email gate: Identity's guest flow proves an address by code, with no membership; only clients allowed guests may ask
  for it.

## C12 · Revocation signals and lifecycle events

P1, receiving:

```csharp
services.AddIdentitySignalReceiver(o =>
{
    o.Audiences.Add("https://files.ocharo.example/signals/identity");   // one per tenant host
    o.TrustedIssuers.Add(new TrustedIssuer { Issuer = "https://id.ocharo.example/" });
});
services.AddTokenDenyList();
app.MapIdentitySignals("/signals/identity");
```

- one route for every tenant: the signature's `iss` names the issuer, and its `aud` the tenant host's signal URL.
- `SessionEndedEvent` and `AccessChangedEvent` feed the deny list, which refuses a revoked person's token until it would
  have expired.
- the deny list sits in `IDistributedCache`, in memory while Files runs one process.
- long work accepted for a person runs as the service and declares its reactions; the defaults hold: sign-out
  continues, access loss cancels.

P2, reactions:

| Event | Files reacts | Needs |
|---|---|---|
| `AccessChangedEvent` `Removed` | revokes the member's shares; hands their files to the organization | provided |
| an organization deleted | soft-deletes its files, purges after retention | an Identity event: `Removed` is per person |
| a person's erasure | erases their personal files, reports completion | an Identity event distinct from `Removed`, which reads like leaving |

- a receiver over `ITrustedIssuerResolver` is a P2 ask, so a new tenant's signals take no restart.
- t8's `person.disabled` drops: a sign-out is no disablement, and a removed member's shares end with the hand-over.

## C13 · Avatar port (Identity P2)

- Identity keeps its profile-picture store port in its `Module`; lifting it into `Identity.Abstractions` lets Files ship
  an adapter package that references `Identity.Abstractions` and Files' contract only.
- Identity never references Files.

## C14 · Tenant onboarding on B

| Step | Identity instance (e.g. Ocharo's) | B |
|---|---|---|
| 1 | registers the resource (C3) | adds the tenant row: id, hostnames, quota |
| 2 | registers each app with its scopes and grants (C4, C6) | adds the trusted issuer: `Issuer`, `Audiences`, `Partition` (C8) |
| 3 | sets the resource's `SignalUrl` (C12) | lists the tenant's signal URL; no shared secret, the signal is key-signed |
| 4 | gives its roles `files:*` permissions (#23) | issues API keys only for callers outside Identity (C9) |

- P1 runs these by configuration; P2 by a tenant admin API under `files.tenants`, which Wheelhouse drives.

## C15 · Subjects across issuers

- on B, `sub` alone is not unique: Files keys people as (`iss`, `sub`) in owners, audit and shares.
- Identity keeps `sub` stable and never reused within an instance.

---

## Policy shape (#23)

| Caller | Needs |
|---|---|
| service (`PrincipalKind.Service`) | the scope |
| API key | the key's scope; tenant and organization from the key |
| person, direct or exchanged | the scope `files.{action}` and the permission `files:{action}`; organization-visible files also need a matching `org_id` |

- permissions: `files:read`, `files:write`, `files:share`, `files:admin`; each tenant's Identity grants them to its roles,
  and Files names no role (`files-integration` #36).
- record rules (owner, organization, visibility) run against Files' rows, never against lists in a token.
- one policy per action over `ICurrentPrincipal`, so no policy branches by scheme.
- `RequireLiveToken()` guards only named dangerous actions: a tenant's or organization's purge, with the P2 admin API.

---

## Changed since 2026-10-26

| 2026-10-26 | Now | Why |
|---|---|---|
| C5 acting headers with `files.delegate` | token exchange (C6), in P1 | `api-communication.md`: token exchange when acting for a person |
| C6 in P2 | P1 | the only way a person's call reaches Files |
| scope alone authorizes a call | a person's call needs scope and a `files:` permission | `api-communication.md` § What a call may do |
| C3 audience = the instance's base URL | the tenant's own host on B | a tenant move becomes a DNS change (#35) |
| C3 `Identity:Resources` keyed by name, no slash | `Identity:Server:Resources` list, URI with its slash | Identity as built |
| C2 `azp` | `client_id`, plus `partition` | Identity as built |
| C8 `AddIdentityValidation`, a resolver from day one | `AddJwtBearerAuthentication` with `TrustedIssuers`; the resolver in P2 | Identity as built; P1 tenants are configuration |
| C9 key prefix, new record fields | no prefix; the fields exist | a hash lookup finds the tenant |
| C12 4 events over Standard Webhooks, a secret per subscriber, a route per tenant | 2 signed signals, one route, no secret; 2 lifecycle events asked for P2 | `api-communication.md` § Revocation signal |
| C12 `person.disabled` | dropped | a sign-out is no disablement |
| C13 `IAvatarStore` in Abstractions | Identity's store port, lifted later | Identity as built |
