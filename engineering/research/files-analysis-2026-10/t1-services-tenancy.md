# T1 — Services, roles and instance kinds

*Last updated: 2026-10-26*

> How many deployables File Storage needs, and how one image serves both an owner's own instance (A) and a shared
> files host that many products point at (B): one image, three roles, two tenancy modes, and a tenant that moves
> between instances without breaking a link.

- Phases P1–P3 follow the [spec](../../../product/product.md); `(U)` = unverified.

---

## 1 · Deployables

| Unit | Kind | Does | Phase | Scales with |
|---|---|---|---|---|
| `api` role | our image | metadata, upload sessions, signed URLs, shares, tenants, admin API, console | P1 | requests |
| `worker` role | our image | variants, previews, archives, scans, migrations, orphan and purge sweeps | P1, in-process | CPU |
| `delivery` role | our image | public `/m/*`, share pages `/s/*`, signed GETs, range requests, on-the-fly transforms | P1, in-process | bandwidth |
| PostgreSQL | stock | metadata, jobs, audit, tenants | P1 | rows |
| byte store | external | object storage or local disk, per D6 (`wow-two-ws/docs/platform-decisions.md`) | P1 | bytes |
| ClamAV | sidecar | malware scan before a file turns `ready` | P2 | uploads |
| Gotenberg | sidecar | Office → PDF for previews | P2 | documents |
| FFmpeg | binary in the worker image | video posters, audio waveforms; transcodes in P3 | P2 | media minutes |

- one image of ours; `Files:Roles = api,worker,delivery` picks what a process runs; P1 runs all three in one process.
- roles split only when load asks: `delivery` onto the high-bandwidth box or behind a CDN, `worker` onto a CPU box.
- no Redis, broker or tus server of our own: jobs ride the be sdk `Jobs/` seam (Hangfire adapter on PostgreSQL (U)),
  tus runs in-process through tusdotnet.
- count: 1 image, 2 stock dependencies in P1, up to 2 sidecars in P2.

---

## 2 · Instance kinds

| | A · owner instance | B · shared files host |
|---|---|---|
| tenants | one: the owner's organizations and apps | many: products of any owner |
| `Files:Tenancy:Mode` | `Single` | `Shared` |
| trusts | the owner's Identity instance | one issuer per tenant, plus tenant API keys |
| bytes | the owner's bucket | per tenant: a prefix in B's store, or the tenant's own bucket |
| hostnames | the owner's `files.{domain}` | each tenant's `files.{domain}` CNAME, plus B's default host |
| quotas | per organization and app | per tenant, then organization and app |
| console | the owner's admins | the operator (us), plus tenant admins scoped to their tenant |
| runs on | beside the owner's products | a storage-fit server: small CPU, large disk or object storage, cheap bandwidth |
| first one | Ocharo, after its test move off B | WoW2's portfolio and Ocharo: Harvest, Events, ventures starting out |

- B is A with more tenant rows: same image, same API, same SDK client; a product changes only base URL and
  credentials.
- every instance keeps a `tenants` table; `Single` caps it at one row and resolves it without asking.
- tenant resolution, first match wins: API key prefix → token `iss` + `aud` → request host (public delivery).
- Form 3 ("provided service, run once by us") becomes B; it runs off the control host, which identity Q4 keeps free
  of products.
- WoW2's own instance is the first B; Ocharo starts on it too and moves to its own A later, as the first
  test migration, run through Wheelhouse (developer, 2026-10-26; [T9](t9-wheelhouse-migration.md)).

---

## 3 · B's two jobs

### Start small

- a new product starts on B: no bucket, no storage code, no large server; its tables hold file ids only.
- as it grows it picks one: stay on B with a larger quota, bring its own bucket to B, or move to its own A.

### Migrate

- into A or B from legacy stores: Ocharo `bytea`, `App_Data/blobs`, local folders, Git LFS, any S3 bucket, a URL list.
- between instances: B → A when a product needs isolation; A → B to consolidate a quiet product.
- across byte stores: B re-points a tenant from one provider to another (Contabo → R2) with a background copy.
- from Form 1: a product that stored files through the library imports its own bucket the same way.

---

## 4 · Moving a tenant

1. pause or dual-write: P1 pauses the tenant's uploads for the copy; P2 writes new uploads to both.
2. metadata: the source signs an export bundle (files, references, variants, shares, quotas, audit tail); the target
   imports it.
3. bytes: server-to-server streams, resumable, each checked by SHA-256; skipped when the target re-points at the
   tenant's own bucket.
4. cutover: the tenant's `files.{domain}` CNAME moves; the source answers `308` to the target for a grace window.
5. cleanup: the source purges the tenant after its retention window.

- links survive: file ids (UUIDv7) and content keys never name an instance, and public URLs sit on the tenant's host.
- signed URLs live minutes, so none outlives a move; share tokens move with the metadata.
- the same export and import serve backups to a second provider (spec § Backing store).

---

## 5 · Where B runs

- shape: 2–4 vCPU, large disk or object storage, unmetered or cheap egress — the opposite of a products host.
- option 1: a storage VPS or storage-dense dedicated server, bytes on local disk; SeaweedFS once a second disk or
  node needs replication.
- option 2: a small VPS in front of object storage (Contabo, Cloudflare R2, Hetzner Object Storage); prices re-checked
  before ordering.
- the pick folds into D6: B's byte store and A's default are one decision.
- public traffic on B goes behind a CDN in P2; egress is the cost line to watch.

---

## 6 · Risks

- noisy neighbour: one tenant's burst starves the rest → per-tenant rate limits and bandwidth caps (P2).
- blast radius: B down takes every tenant's files down → Monitoring health checks; a second B before the fifth
  paying tenant (P3).
- cross-owner trust: B trusts several issuers → a conformance test proves a token never resolves another tenant.
- residency: a tenant that must stay onshore gets its own in-region bucket on B, or its own A (identity Q6).
- abuse: free tenants on B host malware or piracy → scan, takedown flow and per-tenant suspension (P2).
