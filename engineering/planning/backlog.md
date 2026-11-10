# wow-two-sdk-beta.files — Backlog

*Last updated: 2026-11-10*

Every unbuilt item, grouped; top of each group = next. Shipped work lives in [`version-track/`](version-track/).
Shape: wow-two-ws/conventions/planning/version-track/version-track.md

- Source: the feature vector [T2](../research/files-analysis-2026-10/t2-feature-vector.md). `P1` · `P2` · `P3` in
  Notes is the spec's tier (the Ocharo slice · the rest of the vector · a named consumer first), not a version.
- Items the active version holds are in its doc, not here.
- Next: the whole-vector pass over every `P1` row below opens v0.2, the admins' console layout (§ Console) first;
  parked until the Events integration closes (developer, 2026-11-10: "let's hold off whole vector pass for now - add
  to backlog - will come back to it after events server").

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
| Trusted issuers from the tenants table, so a new tenant takes no restart | engineering | P2; files-integration #15, #19; signals read the same resolver since Identity `10.0.30-beta`; [T8](../research/files-analysis-2026-10/t8-identity-contracts.md) C8 |
| API keys from a keys table with a unique hash index | engineering | P2; files-integration #16; P1 reads the hashes from configuration |
| Move the fe upload queue from fe `foundation/uploads` into `fe-files` | engineering | P1; `filePicker` and `imageCropEditor` stay in fe |

## Tenancy and migration

| Item | Type | Notes |
|---|---|---|
| `Shared` tenancy: many tenants on one instance | feature | P1; [T1](../research/files-analysis-2026-10/t1-services-tenancy.md) |
| Per-tenant hostnames and byte store target | feature | P1 on B; one tenant on the `app` site's host until host aliases (files-integration #40) |
| Custom hostname per tenant with TLS | feature | P1; Ocharo's `files.{domain}` from day one |
| Import from S3 buckets, URL lists and Git LFS | feature | P2; folders, SQLite and PostgreSQL columns ship in P1 ([imports](../../product/features/imports.md)) |
| Import freeze, cutover, rollback and retire | feature | P2, with Ocharo prod; files-integration #38 |
| Tenant export and import between instances | feature | P2; signed bundle plus byte copy or re-point |
| Dual-write and read-through during a move | feature | P2 |
| Bring-your-own bucket per tenant | feature | P2 |
| Cross-provider re-point with background copy | feature | P2 |

## Image variants

| Item | Type | Notes |
|---|---|---|
| Named variant presets beyond an app's widths | feature | P2; P1 makes WebP at each app's widths |
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
| Sign-in gate: members of an organization only | feature | P2; files-integration #18; needs Identity's runtime scheme resolver |
| Email gate: allowed addresses prove themselves by a code | feature | P2; files-integration #18; Identity's guest email proof |
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
| How another service reads, edits and writes a file back (AI, image editing): an edit that loses nothing (rotate) rewrites the file and keeps the previous version for undo; tries that branch (filters) save as new files linked to their source; analyse with the developer before building | analysis | P1 for the analysis; developer 2026-11-09: "how another service will be able to edit and write the file ? can file server serve different versions of the same file ?" |
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
| A removed member's shares revoked, their files handed to the organization | feature | P2; files-integration #19; on `AccessChangedEvent` `Removed` |
| An organization's files soft-deleted when Identity deletes it | feature | P2; files-integration #19; needs an Identity organization-deleted event |
| CLI: export, move tenant, verify | feature | P2 |
| S3-compatible gateway for rclone and backup tools | feature | P3 |

## Console

| Item | Type | Notes |
|---|---|---|
| Usage by tenant, organization and app; largest and recent files | feature | P1 |
| Console layout for a tenant's admins (Ocharo first): storage used by file category and by app, largest files, the tenant's policies and configs; the layout is reviewed with the developer before it's built | design | P1; developer 2026-11-09: "necessary for ocharo admin to check files, file server storage and be able to see category of files taking the much space and for different configs" |
| File browser with preview, download, delete, restore | feature | P1 |
| Audit view | feature | P1 |
| Shares: list and revoke | feature | P1 |
| Console sign-in through Identity's BFF | feature | P1 with the console: Ocharo's admins through Ocharo's Identity, one static scheme scoped to `ocharo`; the operator by configuration; tenant admins through their own issuer P2; files-integration #17, #29 |
| Tenants on B: create, keys, issuers, hostnames, suspend | feature | P2; P1 by configuration; files-integration #21 |
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
| fe `FileImage`: one file reference renders `srcset` over the image variants, with `sizes`, intrinsic size and lazy loading; apps never build file URLs | feature | P2; the developer, 2026-11-09: libs on top of Files that apps use; the first consumer is the brand logo: `<BrandLogo :height>` over `FileImage` picks the slot by theme, falls back to the master logo and then the name, and replaces each app's own feed fetch and `<img>`; `BrandLogo` ships in the core fe SDK until a brand SDK is extracted for product components (developer, 2026-11-09, design confirmed) |
| Vector logos: SVG accepted and served sandboxed, so a logo stays sharp at every size | feature | P2; with the Brand app's upload rule (PNG, JPEG and WebP today) |
| Avatar adapter for Identity's profile-picture port | feature | Identity P2; files-integration #20; once Identity lifts the port into `Identity.Abstractions` |

## Naming

| Item | Type | Notes |
|---|---|---|
| Rename Files to a better product name: repo, packages, image, catalog and every consumer | engineering | Later; the developer, 2026-11-09, as Harvester was renamed |

## Operations and compliance

| Item | Type | Notes |
|---|---|---|
| Backups: bucket versioning plus a nightly copy elsewhere | engineering | P1; D6 picks the store |
| Personal-data export and erasure per person | feature | P2; files-integration #19; needs an Identity erasure event distinct from `Removed` |
| Abuse report and takedown on B | feature | P2 |
| Restore drill: one tenant from backup | engineering | P2 |
| Per-tenant encryption keys | feature | P3 |
