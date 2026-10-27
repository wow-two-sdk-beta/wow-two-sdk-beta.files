# T11 — Next work, and shares as containers

*Last updated: 2026-10-27*

> Three asks from the v0.1 review: what Files builds while Identity and the Wheelhouse migration module are still in
> other chats; how a person selects files and shares the selection ("containers"); and what the product direction —
> a file service for products, not a drive — changes in the backlog.

- Evidence: [backlog](../../planning/backlog.md), [T2](t2-feature-vector.md) § 9 Sharing and § 7 Compression,
  [T8](t8-identity-contracts.md), [T9](t9-wheelhouse-migration.md), [T10](t10-console.md) § 5–6,
  [contract](../../architecture/contract.md) § Brokers and § Hosting routes, the v0.1 code (2026-10-27).
- `(U)` = unverified.

---

## 1 · Direction

Files serves files to products: a product's backend uploads, references, links and shares them, and its own UI shows
them. A person-facing drive (folders, sync, versions, per-person grants) is a separate server, built later on the same
packages.

| Consequence | For the backlog |
|---|---|
| a file belongs to a tenant and an app; a person is an opaque `subject` the product passes | nothing new: v0.1 already works this way |
| expiry, references, retention stay core | kept; the drive reuses them |
| shares stay: a product lets its users share what they selected | kept, P1 (§ 3) |
| drive-shaped rows wait for the drive | parked: folders as virtual paths, folder ACLs and per-person grants, versions of a named file, collections and albums, folder upload with paths, file request into a folder |

---

## 2 · What builds now

Identity ships the console sign-in, per-tenant issuers and org gates; the migration module ships the Ocharo import.
Everything below needs neither.

| Slice | Holds | Blocked by | Note |
|---|---|---|---|
| Console (T10 § 6) | Overview, Files, File detail, Usage, Audit, File types, Sign-in by admin key | nothing | daily storage rollup from day one (T10 U3, answered 2026-10-27) |
| Shares | containers of § 3: link, gates, share page, download all as ZIP | nothing | needs the streamed ZIP below |
| Archives | streamed ZIP of many files, archive guards | nothing | P1; shares and console use it |
| Upload tickets | backend-minted browser uploads, sessions that expire | nothing | P1; the product's backend holds the key |
| Image variants | thumbnail, preview, WebP, EXIF strip, BlurHash, bomb guard | core `Media/Images` moving to `Files.Images` | P1; a be sdk release first |
| Safety | SVG and HTML forced download or sanitizing | nothing | P1; v0.1 already forces active types to download |
| Events | uploaded, deleted, share opened on the local bus through the outbox | nothing | P1; be sdk `Messaging/` |
| — | console sign-in by Identity, org and email gates, per-tenant issuers | Identity | contracts stay in T8 |
| — | Ocharo import, CLI mover, import tokens, shared-host deploy | migration module, D6 | T9 |

Proposed order: v0.2 Console (T10 U7), then v0.3 Shares with archives. The rest follows by consumer need.

---

## 3 · Shares as containers

A **share** is the container: a set of files the owner picked, behind a link and zero or more gates. The owner adds or
removes files later and the link shows the current set; no separate collection entity exists.

| Part | Design |
|---|---|
| model | `ShareModel`: id, tenant, app, owner `subject`, title, files (ordered), gates, created, expires, revoked, downloads |
| link | `/s/{token}` on the content host; the token is 22+ URL-safe characters, stored hashed (contract) |
| gates, P1 | expiry, password (Argon2id, attempt limit), max downloads, one-time, revoke |
| lifetime | each file in a share carries the reference `share:{id}`: a shared file outlives its last app reference until the share expires or is revoked, then detaches as any reference does |
| page | file list with icons, preview for images and PDF, download one, download all as one streamed ZIP |
| API | `POST·GET /shares`, `PATCH /shares/{id}` (title, gates, add and remove files), `DELETE /shares/{id}` (revoke); scope `files.share` |
| broker | `IFileSharesBroker`: `CreateAsync` · `UpdateAsync` · `RevokeAsync` · `ListAsync`, conformance in Module and Client |
| audit | share created, opened, downloaded, revoked; refusals with the gate that refused |
| events | `ShareCreated` · `ShareOpened` · `ShareDownloaded` · `ShareRevoked` (contract) |
| limits | files per share (500), ZIP total bytes per policy; a share mixes apps only for a tenant admin |

- a product shows its own "Share" button: its backend creates the share with the selected `FileId`s and hands the link
  to the person.
- a deleted file leaves every share it was in; a share whose last file is gone answers "nothing here", not 404.
- a share by record — "everything `order:123` references" — is a later source for the same model (P2), not a new entity.

---

## Points

Open decisions, parents before children. **F** = a fork only the developer can settle; **V** = a claim to validate.
Format: question → pick.

- [x] N1 · F · a "container": one share whose file set the owner edits, or a collection entity that shares point at → one share (§ 3) — answered 2026-10-27: one share
- [ ] N2 · V · park the drive-shaped backlog rows (§ 1 table) until the drive exists → park
- [ ] N3 · F · after v0.1: v0.2 Console then v0.3 Shares, or Shares first → Console first (§ 2)

### Self-served

- a shared file stays alive through a `share:{id}` reference, so expiry and sweep need no share-specific code.
- the share page lives in `apps/web` on the content host, outside the console shell (T10 § 1).
