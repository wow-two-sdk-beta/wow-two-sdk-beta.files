# wow-two-sdk-beta.files — Architecture

*Last updated: 2026-10-26*

One contract, two registrations, one image in three roles, two tenancy modes.

```
product Domain / Application ──► Abstractions (contract)
product composition root ──► Module (Form 1) + Server routes   or   Client (Forms 2/3)
Module ──► Formats · Compression · Images · Documents (edges) ──► core Storage/ IBlobRepository ──► bucket or disk
service/Files.Api ──► Module + Server (ProjectReference), the HTTP mirror, the console and share pages (apps/web)
Client ──HTTP──► service/Files.Api (api role) ──► Module ──► the instance's store
browser ──signed URL──► Server delivery routes on files.{domain} (delivery role)
```

| Form | Registration | Store owner |
|---|---|---|
| 1 · library | `services.AddFiles()` + the module's migrations; Server routes mapped in the product's host | the consumer |
| 2 · self-hosted | `services.AddFilesClient(…)` + a `files` container, `Single` tenancy | the owner's container |
| 3 · provided | `services.AddFilesClient(…)` against the shared files host, `Shared` tenancy | the shared host |

## Roles

| Role | Runs | Scales by |
|---|---|---|
| `api` | the HTTP API, uploads, admin, console | requests |
| `worker` | variants, archives, sweeps, purges, imports | queue depth |
| `delivery` | `/f`, `/m`, `/s`, `/r` on the content host | bandwidth |

- one image, one process with all three roles in P1; a role flag splits them later.

## Tenancy

- every instance holds a tenants table; `Single` mode has one row, `Shared` mode many.
- a tenant owns its key prefix, policies, quotas, hostname, trusted issuers and API keys.
- storage keys start with the tenant, so a tenant moves by an export bundle plus a byte copy or a bucket re-point.
- a tenant's bytes are stored once per SHA-256 (`files_blobs`); every upload is its own row (`files_files`), so a
  rename or delete never touches shared bytes.
- one process per store in P1: striped in-process locks keep the sweep from deleting bytes an upload is reusing; a
  second process needs a database lock first.

| Project | Responsibility |
|---|---|
| `packages/Abstractions` | the contract — brokers, models, constants, scopes, errors; no DI, options or I/O |
| `packages/Formats` | the file-type registry, sniffing, kinds, safe names |
| `packages/Module` | Form 1 over the module's store and core `Storage/`; embedded SQL migrations |
| `packages/Client` | Forms 2/3 over the service's HTTP API |
| `packages/Server` | delivery, share and request routes, upload protocols, tenancy and admin API |
| `packages/Testing` | fakes and the conformance suite |
| `service/Files.Api` | hosting, the HTTP mirror of the contract, the console and share pages |
| `fe-files` | the TypeScript contract mirror, upload kit, previews, share UI, `/icons` |
| `apps/web` | the console (`/`) and the hosted share and request pages, on `fe-files` through its exports |

- planned packages and when they land: [package-registry.md](package-registry.md).
## Callers

| Caller | Proves itself with | Tenant from |
|---|---|---|
| a product server on Form 1 | its own host's auth; the module trusts its process | `IFilesCallerService`: be sdk tenancy or `DefaultTenantId` |
| a product server on Forms 2/3 | a tenant API key (`X-Api-Key`) or a client-credentials token | the key's tenant, or the trusted issuer's |
| a browser | a signed or public URL on the content host | the URL |

- Identity is reached through contracts only until the final integration: offline token validation (M8), client
  credentials (M10), token exchange later (M11) — [T8](../research/files-analysis-2026-10/t8-identity-contracts.md).
