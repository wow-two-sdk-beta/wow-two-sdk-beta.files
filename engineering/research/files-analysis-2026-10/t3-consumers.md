# T3 — Consumers: what products store today

*Last updated: 2026-10-26*

> Which WoW2 products keep files, where, and what that asks of the first slice. No product uses the be sdk
> `Storage/` module; every one that keeps files hand-rolls a local folder, a database column or its own S3 wiring.

- Sweep of `workbench/` on 2026-10-26, read-only; Ocharo's numbers come from the [spec](../../../product/product.md)
  § Problem. `(U)` = unverified. Paths are relative to each repo's `engineering/codebase/`.

---

## 1 · Products that keep files

| Product | Files | Bytes live | Limits | Delivery | Transforms | Evidence |
|---|---|---|---|---|---|---|
| Ocharo (5 apps) | media, exports, sourcing, brand, blog, models | `bytea`, `App_Data/blobs`, blob folders, state JSON, Git LFS | 15 / 20 / 30 / 64 MiB | through each API | WebP previews in 3 `MediaStore` copies | spec § Problem |
| ListingShelf → Harvest | listing photos, gzipped page HTML, JSON exports | local `photos/`, `exports/`; HTML in a `byte[]` column | 5 MB a photo, 12 a capture, 90-day retention | `GET api/photos/{hash}`, immutable, 1 year | SHA-256 dedup, gzip | `ListingShelf.Infrastructure/Listings/Settings/PhotoSettings.cs:7` |
| Matinee | films, posters, device copies | local `media/`, `posters/` | 5 MB poster | range streaming; posters 7 days | none | `Matinee.Api/Media/Controllers/MediaController.cs:31` |
| ForeverPin | a code's centre logo | base64 data URL in `StyleJson` (jsonb) | none seen (U) | rendered per request | QR overlay (be `Codes/`) | `ForeverPin.Domain/Codes/Core/Entities/CodeEntity.cs:35` |
| Wheelhouse | pre-deploy snapshots, release bundles, submissions | folders on the target host | 3 MB release fetch, keep 5 / 10 / 10 | private | tar, zip, `pg_dump` | `wheelhouse.runner-services/runner.py:43` |
| Podcast Readiness | backup folders with checksums | local folder, newest 7 | — | private | SQLite backup, SHA-256 | `PodcastReadiness.Persistence/Backups/SqliteBackupWriter.cs:64` |
| TNIS | ingested datasets, baked GeoJSON feeds | local `data/build/` | — | `PhysicalFile` with `ETag` | none | `Tnis.Api/Network/Core/Controllers/NetworkController.cs:34` |
| Home Atlas | reference previews, texture atlases | local `private-media/`, read-only | — | API-served, no range | none | `HomeAtlas.Persistence/ReferenceMedia.cs:36` |
| Your Pocket Doctor (stale, net8) | profile photos, medical scans | local folder + DB row; Django on S3 | 10 MB | static files; public S3 | OCR | `core-service/…/LocalFileStorageBroker.cs:35` |

---

## 2 · Transient processing (nothing kept)

- pdf-editor: 256 MB uploads → merge, split, redact, OCR, DOCX, DjVu, encrypt → response; ZIP for split and Bates.
- TranscriptForge: 2 GiB uploads → temp folder → FFmpeg chunks → transcript text; project export ZIP built on the fly
  (`TranscriptForge.Application/Transcripts/Core/Constants/TranscriptUploadConstants.cs:7`).
- PbnStudio, EPUB Review, PRISM, Whiteout and five SaaS pilots: browser-only reads and JSON / PNG / PDF / ZIP downloads.
- Museums Gallery: photo uploads planned only.

---

## 3 · What recurs

- content-addressed images with immutable public caching: ListingShelf, Matinee, Pocket Doctor, Home Atlas — each
  with its own cap and retention, none with thumbnails or WebP.
- large uploads with hand-rolled limits: TranscriptForge 2 GiB, pdf-editor 256 MB, Matinee films with range and
  resumable copies.
- ZIP and keep-N output with checksums: pdf-editor, TranscriptForge, Podcast Readiness, Wheelhouse.
- no product issues a signed or presigned download today.

---

## 4 · What it means for the slice

| Consumer | Form | Needs in P1 |
|---|---|---|
| Ocharo | B, then its own A | everything in the spec's Ocharo slice; import from `bytea` and blob folders |
| Harvest (from ListingShelf) | B | content-hash photos, immutable public URLs, references with retention → orphan sweep and lifecycle rules move to P1 |
| Events catalogs | B | public media with variants (decided 2026-10-25) |
| ForeverPin | B, optional | small public logos out of jsonb |
| pdf-editor, TranscriptForge | library edges | `Files.Compression` (streaming ZIP), `Files.Documents`, upload limits; tus when TranscriptForge keeps files |
| Wheelhouse, Podcast Readiness | none in P1 | backups stay local; an off-host backup target is a P3 Files use |
| Matinee, Home Atlas, TNIS | none | local desktop or read-only data; `Files.Formats` at most |

- T2 changes from this: lifecycle rules (expire after N days) and the orphan sweep move from P2 to P1 for Harvest.
- tus stays P2: no consumer keeps a file over 100 MB yet.
