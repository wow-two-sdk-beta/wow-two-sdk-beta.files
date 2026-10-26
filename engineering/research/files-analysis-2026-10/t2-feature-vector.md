# T2 — Feature vector

*Last updated: 2026-10-26*

> Every capability a files domain may need, grouped and phased, so the whole vector lands in the backlog and the
> build takes only the slice products need now. P1 = the Now slice (Ocharo plus the products in
> [T3](t3-consumers.md)); P2 completes the vector after the first release; P3 waits on a named consumer.

- Benchmarked from memory of Cloudinary, Uploadcare, Filestack, UploadThing, ImageKit, Supabase Storage, Firebase
  Storage, S3, Dropbox and WeTransfer transfers, Nextcloud shares; `(U)` = unverified against current docs.
- `be` = an existing be sdk part; `fe` = an existing fe sdk part.

---

## 1 · Store and lifecycle

| Feature | Phase | Notes |
|---|---|---|
| `files` metadata: id, tenant, organization, app, owner, name, type, size, SHA-256, visibility, state, timestamps | P1 | spec § Store |
| content-hash keys, dedup within a tenant, never across | P1 | `{tenant}/{sha[..2]}/{sha}` |
| references: a file lives while a record points at it, orphan sweep | P1 | Harvest prunes captures ([T3](t3-consumers.md)) |
| soft delete, trash, restore, purge after retention | P1 | retention per tenant policy |
| folders as virtual paths, move, rename, copy | P2 | products that browse files (console, Desk) |
| tags and custom metadata (JSON) per file | P2 | searchable keys |
| versions of a named file, restore a version | P2 | |
| lifecycle rules: expire after N days | P1 | Harvest's 90-day captures, exports, temp files |
| lifecycle rules: move cold files to a cheaper class | P2 | |
| storage classes: hot disk, object storage, archive | P3 | |
| legal hold and retention lock | P3 | |
| integrity scrub: re-hash a sample, repair from the backup copy | P2 | |

---

## 2 · Upload

| Feature | Phase | Notes |
|---|---|---|
| API-proxied multipart upload | P1 | be HMAC `Transfer/` routes take 100 MB |
| backend-minted upload tickets: the product's backend asks, the browser uploads | P1 | the product needs no files-audience user token |
| direct presigned PUT to object storage, finished by a server check | P2 | be `IBlobUrlIssuer` S3 presign |
| S3 multipart for large direct uploads | P2 | |
| resumable uploads: tus 1.0 (tusdotnet) | P2 | GLB, video, ZIP over 100 MB; TranscriptForge's 2 GB |
| upload from URL: the server fetches | P2 | SSRF guard: no private ranges |
| folder upload, keeping relative paths | P2 | |
| idempotency key per upload | P1 | retries never duplicate |
| upload sessions expire; abandoned parts are swept | P1 | |
| client-side resize and compress before upload | P2 | fe crop exists |
| file requests: an anonymous upload link into a folder | P2 | sharing § 9 |

---

## 3 · Validation and safety

| Feature | Phase | Notes |
|---|---|---|
| size limit, extension and MIME allowlist per app policy | P1 | replaces the 15 / 20 / 30 MB copies |
| magic-byte sniffing: the content must match the claimed type | P1 | `Files.Formats` ([T6](t6-library-shape.md)) |
| filename sanitizing, Unicode normalization, length cap | P1 | fe `FileName.ts` exists |
| image limits: max pixels, decompression-bomb guard | P1 | be `ImageRejectedException` exists |
| archive guards: entry count, ratio, total size, path traversal | P1 | with any extract |
| malware scan through ClamAV, file stays `pending` until clean | P2 | state machine below |
| SVG and HTML sanitizing, or forced download | P1 | user content never runs on an app origin |
| content moderation (NSFW, CSAM hash match) | P3 | B with public tenants |
| file states: `uploading` → `pending` → `ready` · `rejected` · `quarantined` | P1 | scan joins in P2 |

---

## 4 · Images

| Feature | Phase | Notes |
|---|---|---|
| variants on upload: thumbnail, preview, WebP, named presets per app | P1 | be `IImageService` (SkiaSharp) |
| EXIF strip on public images, orientation fix | P1 | be `ExifMetadataMapper` |
| BlurHash and dominant color for placeholders | P1 | be exists |
| perceptual hash for near-duplicate finding | P2 | be exists |
| AVIF output | P2 | Skia AVIF encode support (U) |
| on-the-fly transforms by signed URL params: size, fit, crop, format, quality, DPR | P2 | own renderer, cached variants |
| smart and face-aware crop | P3 | |
| watermark per share or per tenant | P2 | be exists |
| background removal as a variant | P2 | be `Media/BackgroundRemoval` (HTTP worker) |
| HEIC input from iPhones | P2 | decoder support in Skia (U) |
| RAW photo preview | P3 | |

---

## 5 · Documents

| Feature | Phase | Notes |
|---|---|---|
| PDF page images: first-page thumbnail, page previews | P2 | PDFium render, on the core roadmap |
| PDF tools: merge, split, compress, encrypt, stamp, form fill | P1 move | be `IPdfService` exists |
| Office → PDF preview (DOCX, XLSX, PPTX, ODF) | P2 | Gotenberg sidecar |
| text extraction for search | P3 | PdfPig exists; OCR later |
| Markdown, code and text preview with syntax highlight | P2 | fe preview |
| e-book cover and metadata (EPUB) | P3 | |

---

## 6 · Video and audio

| Feature | Phase | Notes |
|---|---|---|
| probe: duration, codecs, dimensions | P2 | FFmpeg `ffprobe` |
| poster frame and sprite sheet for scrubbing | P2 | |
| audio waveform image | P2 | |
| range requests for playback | P1 | delivery |
| transcode to H.264 / AAC MP4, and HLS ladders | P3 | a named consumer first (Matinee, PoseCoach clips) |
| subtitles side files (VTT, SRT) next to a video | P3 | be `Captions/` parses them |

---

## 7 · Compression and archives

| Feature | Phase | Notes |
|---|---|---|
| download several files as one ZIP, streamed, no temp file | P1 | console, shares, exports |
| create ZIP, TAR, TAR.GZ as a job, result is a file | P1 ZIP, P2 TAR | |
| extract an uploaded archive into a folder | P2 | with § 3 guards |
| list an archive's entries and preview one without extracting | P2 | |
| read 7z, RAR | P2 | SharpCompress already pinned in the be sdk |
| gzip, Brotli, Zstandard stream helpers | P1 gzip/Brotli, P2 zstd | BCL covers gzip and Brotli; zstd through ZstdSharp (U) |
| transparent compression at rest for text types | P3 | dedup matters more |
| encrypted ZIP (AES) for exports | P3 | |
| lossless image optimization (PNG, JPEG re-encode) | P2 | images § 4 |
| PDF compression | P1 move | be `PdfCompressSpec` exists |

---

## 8 · Delivery

| Feature | Phase | Notes |
|---|---|---|
| private by default: signed URLs, minutes long | P1 | spec |
| public immutable URLs `/m/{sha}/…` with year-long caching | P1 | D7 |
| user content on its own origin: `files.{domain}`, never the app host | P1 | XSS guard |
| `nosniff`, sandbox CSP, attachment disposition for active types | P1 | |
| range requests, `ETag`, `If-None-Match`, `304` | P1 | |
| custom hostname per tenant with TLS | P1 | B's tenants CNAME to it; Ocharo needs it for a later move ([T9](t9-wheelhouse-migration.md)) |
| CDN in front of public files, purge on delete | P2 | spec |
| hotlink protection by referrer per tenant | P2 | |
| bandwidth caps and rate limits per tenant | P2 | B noisy neighbour |
| download counters per file | P2 | |
| image resizing at the edge | P3 | spec |

---

## 9 · Sharing (D7)

A share is a link to a file, a set or a folder, behind zero or more gates.

| Gate or feature | Phase | Notes |
|---|---|---|
| expiry: absolute date or after N days | P1 | |
| password: Argon2id-hashed, attempt limits, lockout | P1 | core hashing |
| max downloads, then the link closes | P1 | |
| one-time link | P1 | max downloads = 1 |
| revoke at any time | P1 | |
| sign-in gate: members of an organization only | P2 | Identity OIDC on the share page ([T5](t5-identity.md)) |
| email gate: allowed addresses or domains prove themselves by a code | P2 | Identity guest code, or Files' own (T5) |
| view only: preview without download | P3 | weak against screenshots; watermark helps |
| watermark on view or download, with the viewer's email | P3 | |
| terms or NDA accept before access | P3 | |
| IP or country allowlist | P3 | be `Geo/` exists |
| share page: branded per tenant, preview, download, download all as ZIP | P1 | fe-files |
| file request: upload-only link into a folder, with its own gates | P2 | |
| access log and owner notification on first open or download | P2 | |
| QR code for a share link | P2 | be `Codes/` exists |
| share analytics: opens, downloads, countries | P3 | |

---

## 10 · Organization and search

| Feature | Phase | Notes |
|---|---|---|
| list by owner, app, type, date; paging | P1 | console browser |
| search by name, tag, metadata keys | P2 | PostgreSQL full-text first (D18) |
| collections and albums across folders | P3 | product-level for most products |
| duplicates finder (exact and perceptual) | P2 | |
| full-text search inside documents | P3 | |

---

## 11 · Access control

| Feature | Phase | Notes |
|---|---|---|
| authorize by token claims: tenant, `org_id`, app, role, permission | P1 | [T5](t5-identity.md) |
| per-file visibility: private, organization, public | P1 | |
| folder ACLs and per-file grants to people | P2 | Desk-style workspaces |
| service-to-service calls by client credentials | P1 | identity M10 |
| tenant API keys on B for products without Identity | P1 | hashed, scoped, prefix-routed |
| token exchange for user-scoped calls | P2 | identity M11 |

---

## 12 · Limits, metering, billing

| Feature | Phase | Notes |
|---|---|---|
| quotas: bytes, files, monthly egress per tenant, organization, app | P2 | spec |
| usage figure on every upload response | P2 | spec |
| soft and hard limits, warning events at 80 % and 100 % | P2 | |
| usage events for billing on B | P3 | D17 |

---

## 13 · Tenancy and migration

| Feature | Phase | Notes |
|---|---|---|
| tenants table in every instance; `Single` and `Shared` modes | P1 | [T1](t1-services-tenancy.md) |
| per-tenant issuer trust, API keys, hostnames, byte store target | P1 on B | |
| bring-your-own bucket per tenant | P2 | |
| import from legacy stores: `bytea`, folders, S3, URL list | P1 | Ocharo cutover |
| tenant export and import between instances | P2 | T1 § 4 |
| dual-write and read-through during a move | P2 | |
| cross-provider re-point with background copy | P2 | |

---

## 14 · Events and integration

| Feature | Phase | Notes |
|---|---|---|
| `IEvent` records on the local bus: uploaded, ready, rejected, deleted, variant ready, share opened | P1 | be `Messaging/` |
| outgoing webhooks (Standard Webhooks) for the same events | P2 | be `AddWebhooks` |
| EF outbox for reliable publishing | P1 | be `Messaging/` outbox |
| S3-compatible gateway so rclone and backup tools speak to an instance | P3 | |
| CLI: import, export, move tenant, verify | P2 | migrations run from it |

---

## 15 · Console

| Feature | Phase | Notes |
|---|---|---|
| usage by tenant, organization and app; largest files; recent uploads | P1 | spec |
| file browser with preview, download, delete, restore | P1 | spec |
| audit view: uploads, deletes, private downloads, share opens | P1 | |
| shares: list, revoke, gates, access log | P1 list and revoke, P2 log | |
| tenants on B: create, keys, issuers, hostnames, quotas, suspend | P1 on B | |
| orphans, failed scans, quarantined files | P2 | |
| migration runner with progress | P2 | |
| policy editor: limits, types, variants, retention | P2 | P1 by config |

---

## 16 · Developer surface

| Feature | Phase | Notes |
|---|---|---|
| one broker set for Module and Client | P1 | [T4](t4-contract.md) |
| ASP.NET helpers: `IFormFile` → upload, `FileId` model binding, typed references | P1 | |
| fe upload kit: dropzone, queue with progress, retry, cancel, resumable | P1 basic, P2 tus | fe `UploadQueue` exists, `fileUpload` empty |
| fe previews: image, PDF, video, audio, text, code, archive listing | P1 image and PDF, P2 rest | fe `imagePreview` exists |
| fe file list and grid, share dialog, gate pages | P1 | |
| file icon pack | P1 | [T7](t7-file-icons.md) |
| test kit: in-memory store, fake scanner, conformance base | P1 | |

---

## 17 · Operations and compliance

| Feature | Phase | Notes |
|---|---|---|
| health checks, OpenTelemetry traces and metrics | P1 | be `Observability/` |
| backups: bucket versioning plus a nightly copy to a second provider | P1 | spec |
| audit row for every upload, delete, private download, share open | P1 | D14 audit capability later |
| personal-data export and erasure per person, fanned out from Identity | P2 | identity Q21 |
| encryption at rest: provider-side; per-tenant keys | P1 provider, P3 per tenant | |
| abuse report and takedown flow on B | P2 | |
| disaster recovery drill: restore one tenant from backup | P2 | |
