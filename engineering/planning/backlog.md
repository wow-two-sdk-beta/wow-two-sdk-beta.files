# wow-two-sdk-beta.files — Backlog

*Last updated: 2026-10-26*

Every unbuilt item, grouped; top of each group = next. Shipped work lives in [`version-track/`](version-track/).
Shape: wow-two-ws/conventions/planning/version-track/version-track.md

- Source: the feature vector [T2](../research/files-analysis-2026-10/t2-feature-vector.md). `P1` · `P2` · `P3` in
  Notes is the spec's tier (the Ocharo slice · the rest of the vector · a named consumer first), not a version.
- Items the active version holds are in its doc, not here.

## Features

| Feature | State | Spec |
|---|---|---|
| File types and icons | planned | — |
| Storage and lifecycle | planned | — |
| References | planned | — |
| Delivery | planned | — |
| Upload | planned | — |
| Validation and safety | planned | — |
| Image variants | planned | — |
| Documents | planned | — |
| Video and audio | planned | — |
| Compression and archives | planned | — |
| Sharing | planned | — |
| Organization and search | planned | — |
| Access control | planned | — |
| Limits and metering | planned | — |
| Tenancy and migration | planned | — |
| Events and integration | planned | — |
| Console | planned | — |
| Developer kits | planned | — |
| Operations and compliance | planned | — |

## Adoption

| Item | Type | Notes |
|---|---|---|
| Extract core `Media/Images` into `Files.Images` | engineering | P1; namespace kept, core forwards one release; SkiaSharp moves with it |
| Extract core `Media/Pdf` into `Files.Documents` | engineering | P1; PDFsharp and PdfPig move with it |
| Adopt the Identity principal seam (C1) once core ships it | engineering | contracts in [T8](../research/files-analysis-2026-10/t8-identity-contracts.md); integration as we go |
| Adopt multi-issuer token validation (C8) | engineering | B trusts each tenant's issuer |
| Adopt the remaining Identity contracts (C2–C15) as they ship | engineering | scopes, audiences, delegation, API-key fields, webhooks |
| Move the fe upload queue from fe `foundation/uploads` into `fe-files` | engineering | P1; `filePicker` and `imageCropEditor` stay in fe |

## Tenancy and migration

| Item | Type | Notes |
|---|---|---|
| `Shared` tenancy: many tenants on one instance | feature | P1; [T1](../research/files-analysis-2026-10/t1-services-tenancy.md) |
| Per-tenant issuer trust, hostnames and byte store target | feature | P1 on B |
| Custom hostname per tenant with TLS | feature | P1; Ocharo's `files.{domain}` from day one |
| Import from legacy stores: `bytea`, folders, S3, URL list | feature | P1; the Ocharo cutover |
| `files` CLI mover image for imports | feature | P1; runs on the source host, read-only mounts (T9) |
| Files-minted import tokens, tenant- and import-scoped | feature | P1; hours long, over stdin (T9 W6) |
| Tenant export and import between instances | feature | P2; signed bundle plus byte copy or re-point |
| Dual-write and read-through during a move | feature | P2 |
| Bring-your-own bucket per tenant | feature | P2 |
| Cross-provider re-point with background copy | feature | P2 |

## Image variants

| Item | Type | Notes |
|---|---|---|
| Variants on upload: thumbnail, preview, WebP, named presets | feature | P1; replaces each app's preview code |
| EXIF strip on public images, orientation fix | feature | P1 |
| BlurHash and dominant color for placeholders | feature | P1 |
| Image limits: max pixels, decompression-bomb guard | feature | P1 |
| Lossless image optimization | feature | P2 |
| Perceptual hash for near-duplicates | feature | P2 |
| AVIF output | feature | P2; Skia AVIF encode unverified |
| On-the-fly transforms by signed URL params | feature | P2; own renderer, cached variants |
| Watermark per share or per tenant | feature | P2 |
| Background removal as a variant | feature | P2; core `Media/BackgroundRemoval` worker |
| HEIC input | feature | P2; decoder support unverified |
| Smart and face-aware crop | feature | P3 |
| RAW photo preview | feature | P3 |

## Sharing

| Item | Type | Notes |
|---|---|---|
| Shares as containers: a link over a file set the owner edits | feature | P1; [T11](../research/files-analysis-2026-10/t11-next-and-shares.md) § 3 |
| Share links with expiry, password, download limits, one-time, revoke | feature | P1; D7 |
| Share page: tenant brand, preview, download, download all as ZIP | feature | P1; fe-files |
| Sign-in gate: members of an organization only | feature | P2; Identity OIDC |
| Email gate: allowed addresses prove themselves by a code | feature | P2 |
| File request: upload-only link into a folder | feature | P2 |
| Access log and owner notification on first open | feature | P2 |
| QR code for a share link | feature | P2; core `Codes/` |
| View-only shares | feature | P3 |
| Watermark with the viewer's email | feature | P3 |
| Terms or NDA accept before access | feature | P3 |
| IP or country allowlist | feature | P3; core `Geo/` |
| Share analytics: opens, downloads, countries | feature | P3 |

## Compression and archives

| Item | Type | Notes |
|---|---|---|
| Download several files as one streamed ZIP | feature | P1; console, shares, exports |
| Create a ZIP as a job, the result a file | feature | P1 |
| Archive guards: entry count, ratio, total size, path traversal | feature | P1 |
| gzip and Brotli stream helpers | feature | P1; BCL |
| Create TAR and TAR.GZ | feature | P2 |
| Extract an uploaded archive into a folder | feature | P2 |
| List an archive's entries and preview one | feature | P2 |
| Read 7z and RAR | feature | P2; SharpCompress |
| Zstandard stream helpers | feature | P2; ZstdSharp unverified |
| Transparent compression at rest for text types | feature | P3 |
| Encrypted ZIP for exports | feature | P3 |

## Upload

| Item | Type | Notes |
|---|---|---|
| Backend-minted upload tickets for browsers | feature | P1; no files-audience user token |
| Upload sessions expire; abandoned parts swept | feature | P1 |
| Direct presigned PUT, finished by a server check | feature | P2 |
| S3 multipart for large direct uploads | feature | P2 |
| Resumable uploads over tus 1.0 | feature | P2; GLB, video, ZIP over 100 MB |
| Upload from URL with an SSRF guard | feature | P2 |
| Folder upload keeping relative paths | feature | P2 |
| Client-side resize and compress before upload | feature | P2 |

## Validation and safety

| Item | Type | Notes |
|---|---|---|
| SVG and HTML sanitizing, or forced download | feature | P1 |
| Malware scan through ClamAV; file stays `pending` until clean | feature | P2; `Files.Scanning` |
| Content moderation: NSFW, CSAM hash match | feature | P3; B with public tenants |

## Delivery

| Item | Type | Notes |
|---|---|---|
| CDN in front of public files, purge on delete | feature | P2 |
| Hotlink protection by referrer per tenant | feature | P2 |
| Bandwidth caps and rate limits per tenant | feature | P2; B noisy neighbour |
| Download counters per file | feature | P2 |
| Image resizing at the edge | feature | P3 |

## Storage and lifecycle

| Item | Type | Notes |
|---|---|---|
| Folders as virtual paths: move, rename, copy | feature | P2 |
| Tags and custom metadata per file | feature | P2 |
| Versions of a named file, restore a version | feature | P2 |
| Lifecycle rule: move cold files to a cheaper class | feature | P2 |
| Integrity scrub: re-hash a sample, repair from backup | feature | P2 |
| Storage classes: hot disk, object storage, archive | feature | P3 |
| Legal hold and retention lock | feature | P3 |

## Documents

| Item | Type | Notes |
|---|---|---|
| PDF page images: first-page thumbnail, page previews | feature | P2; PDFium |
| Office → PDF preview through Gotenberg | feature | P2 |
| Markdown, code and text preview | feature | P2; fe preview |
| Text extraction for search | feature | P3 |
| EPUB cover and metadata | feature | P3 |

## Video and audio

| Item | Type | Notes |
|---|---|---|
| Probe duration, codecs and dimensions | feature | P2; `Files.Video`, FFmpeg |
| Poster frame and sprite sheet | feature | P2 |
| Audio waveform image | feature | P2 |
| Transcode to MP4 and HLS ladders | feature | P3; a named consumer first |
| Subtitle side files next to a video | feature | P3; core `Captions/` |

## Access control

| Item | Type | Notes |
|---|---|---|
| Per-file visibility: private, organization, public | feature | P1 |
| Folder ACLs and per-file grants to people | feature | P2 |
| Token exchange for user-scoped calls | feature | P2; identity M11 |

## Limits and metering

| Item | Type | Notes |
|---|---|---|
| Quotas: bytes, files, monthly egress per tenant, organization, app | feature | P2 |
| Usage figure on every upload response | feature | P2 |
| Soft and hard limits with warning events | feature | P2 |
| Usage events for billing on B | feature | P3; D17 |

## Events and integration

| Item | Type | Notes |
|---|---|---|
| File events on the local bus through the EF outbox | feature | P1; uploaded, ready, rejected, deleted |
| Outgoing webhooks for the same events | feature | P2; core `AddWebhooks` |
| CLI: export, move tenant, verify | feature | P2 |
| S3-compatible gateway for rclone and backup tools | feature | P3 |

## Console

| Item | Type | Notes |
|---|---|---|
| Usage by tenant, organization and app; largest and recent files | feature | P1 |
| File browser with preview, download, delete, restore | feature | P1 |
| Audit view | feature | P1 |
| Shares: list and revoke | feature | P1 |
| Tenants on B: create, keys, issuers, hostnames, suspend | feature | P1 on B |
| Orphans, failed scans, quarantined files | feature | P2 |
| Migration runner with progress | feature | P2 |
| Policy editor: limits, types, variants, retention | feature | P2; P1 by config |

## Organization and search

| Item | Type | Notes |
|---|---|---|
| Search by name, tag and metadata keys | feature | P2; PostgreSQL full-text first |
| Duplicates finder, exact and perceptual | feature | P2 |
| Collections and albums across folders | feature | P3 |
| Full-text search inside documents | feature | P3 |

## Developer kits

| Item | Type | Notes |
|---|---|---|
| ASP.NET helpers: `IFormFile` upload, `FileId` binding, typed references | feature | P1 |
| fe upload kit: dropzone, queue with progress, retry, cancel | feature | P1; resumable in P2 |
| fe previews: image and PDF | feature | P1; video, audio, text, code, archive in P2 |
| fe file list and grid, share dialog, gate pages | feature | P1 |
| Test kit: in-memory store, fake scanner, personas | feature | P1 |

## Operations and compliance

| Item | Type | Notes |
|---|---|---|
| Backups: bucket versioning plus a nightly copy elsewhere | engineering | P1; D6 picks the store |
| Deploy the shared files host through Wheelhouse | product | P1; D6 picks the server |
| Ocharo tenant on the shared host, Catalogue and Brand first | product | P1; through the Wheelhouse migration module |
| Personal-data export and erasure per person | feature | P2; fanned out from Identity |
| Abuse report and takedown on B | feature | P2 |
| Restore drill: one tenant from backup | engineering | P2 |
| Per-tenant encryption keys | feature | P3 |
