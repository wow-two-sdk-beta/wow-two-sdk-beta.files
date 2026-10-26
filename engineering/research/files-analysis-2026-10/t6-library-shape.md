# T6 — Library shape: one files domain

*Last updated: 2026-10-26*

> How wide the domain is and how it splits into packages. The developer asked whether one big library that works
> with files — compression included — should hold the server as one of its parts. It should: one `files` domain repo,
> many small packages, so a product takes only compression or only the client, and the server hosts the rest.

- Layers and naming per domain structure (`wow-two-ws/conventions/development/repo/structure/domain-structure.md`);
  `(U)` = unverified.

---

## 1 · Options

| Option | Repo(s) | Holds | Fits the conventions |
|---|---|---|---|
| A · storage only | `….file-storage` | metadata, uploads, shares, server; processing stays in core `Media/` | yes, as written |
| B · one files domain | `….files` | storage + server + formats + compression + images + documents + video + icons | needs one rule amended |
| C · two domains | `….files` (stateless toolkit) + `….file-storage` (state + server) | the toolkit is referenced one way | yes |

- A leaves compression, formats and icons homeless or in core, which keeps growing; the developer's direction is
  to empty core domain by domain.
- C doubles repos, versions and compat jobs for a toolkit nobody ships without storage soon.
- B matches the ask: one place for everything about files; granular packages keep consumers light.
- **Recommend B.**

---

## 2 · The rule B amends

- domain structure § Scope: "keep a stateless library with heavy dependencies and no contract — media, codes, AI
  providers — in the core repo as a companion package."
- amendment: a stateless library moves into a domain repo when that domain's own pipeline uses it; the rest of its
  family stays in core.
- applied: `Media/Images` and `Media/Pdf` move to Files (variants, previews, PDF tools); `Media/Word`, `Markdown`,
  `Captions`, `YouTube`, `Tabular`, `Csv`, `Excel`, `BackgroundRemoval` stay in core.
- core reads neither `Media/Images` nor `Media/Pdf` (grep 2026-10-26), so they leave cleanly; consumers add a package
  reference and keep their `using` lines (moved code keeps its namespace).

---

## 3 · What stays in core

- `Storage/` stays: byte I/O adapters (local, S3, Azure) and signed-URL issuers are infrastructure like `Data/`, and
  core's own `Messaging/Transport` claim check reads `IBlobRepository`.
- Files builds on `Storage/`: every Files byte goes through `IBlobRepository`, so a new provider adapter lands in core
  and every product gains it.
- `Web/Compression` (HTTP response compression) stays; Files' compression is about files, not responses.

---

## 4 · Packages

`WoW2.Sdk.Beta.Be.Files.{Layer}`, namespace root `WoW.Two.Sdk.Beta.Be.Files`, except moved code keeping its namespace.

| Package | Layer | Holds | Heavy dependency | Phase |
|---|---|---|---|---|
| `Files.Abstractions` | contract | brokers, models, `IEvent` records, errors, `FileKind` | BCL only | P1 |
| `Files.Formats` | edge | extension and MIME registry, magic-byte sniffing, kinds, safe names; generated from the icon registry ([T7](t7-file-icons.md)) | none | P1 |
| `Files.Compression` | edge | ZIP / TAR / gzip / Brotli streams, streaming ZIP download, archive listing and extraction with bomb guards; 7z / RAR read | SharpCompress (pinned), ZstdSharp (U) | P1 ZIP, P2 rest |
| `Files.Images` | edge | `Media/Images` moved: probe, resize, crop, watermark, EXIF, BlurHash, hashes | SkiaSharp | P1 |
| `Files.Documents` | edge | `Media/Pdf` moved; PDF page render; Office → PDF through Gotenberg | PDFsharp, PdfPig, PDFium (P2) | P1 move, P2 render |
| `Files.Video` | extension | FFmpeg wrapper: probe, poster, sprite, waveform, transcode | FFmpeg binary | P2 |
| `Files.Scanning` | extension | ClamAV `INSTREAM` client | none | P2 |
| `Files.Module` | forms | Form 1: metadata in EF, references, variants pipeline, policies, shares, signed URLs, jobs, audit | core | P1 |
| `Files.Client` | forms | Forms 2/3: HTTP client implementing the same brokers | core | P1 |
| `Files.Server` | extension | delivery and share routes, tus, tenancy, admin API | tusdotnet | P1 |
| `Files.Testing` | test kit | in-memory store, fake scanner, personas, conformance base | `…Be.Testing` | P1 |

- the service `service/Files.Api` hosts `Module` + `Server` + the edges; `apps/web` serves the console and share
  pages.
- what a product takes:
  - only compression or format checks → `Files.Compression` / `Files.Formats`, no store.
  - files in its own bucket, no service → `Files.Module` (Form 1).
  - an A or B → `Files.Client` only.

---

## 5 · Frontend

- one npm package `@wow-two-beta/fe-files`, per the one-package-per-repo rule:
  - upload kit: dropzone, queue (moves from fe `foundation/uploads`), resumable transport, progress, retry, cancel.
  - previews: image (fe `imagePreview`), PDF, video, audio, text, code, archive listing.
  - file list and grid, share dialog, gate pages, file request page.
  - icon pack under the subpath `@wow-two-beta/fe-files/icons`, usable in any product without File Storage.
- stays in `@wow-two-beta/fe`: `filePicker`, `fileUploadPicker`, `imageCropEditor`, `imageEditor`, `foundation/files`
  helpers — generic form inputs, per domain structure § Open "fe split" (U, open there too).

---

## 6 · Repo

- name: `wow-two-sdk-beta.files`, following Identity's org (identity Q1a); slug `files`, `{Domain}` = `Files`.
- `wow-two-platform.storage.file` (empty 2024 solution) is archived by the developer; repository deletion and
  archiving stay out of every git flag.
- the repo comes from the domain template the Identity chat is preparing (`wow-two-sdk-beta.domain-template`,
  identity Q35); nothing here scaffolds before that template lands.
- on the first docs commit the spec becomes `product/product.md` and this batch moves to
  `engineering/research/files-analysis-2026-10/`; the hub keeps the catalog entry, the platform-apps row and
  cross-capability decisions.
