# Legacy `wow-two-platform.storage.file`

*Last updated: 2026-10-26*

> What the 2024 repo `wow-two-platform/wow-two-platform.storage.file` held, what Files keeps from it, and what it
> drops. Read 2026-10-26 before the archive; the source stays readable in the archived repo.

## What it held

- `main`: a README and an empty `Backbone.Storage.File.sln` (2024-10-27).
- `dev`, 7 commits ahead (last push 2024-10-31): three projects under `Backbone.Storage.File.*`.

| Project | Holds |
|---|---|
| `Abstractions` | `IStorageFile`: original name, physical name, content type, extension, size |
| `ContentType.Abstractions` | `FileContentCategories` (27), `FileContentTypes` (43, each tagged with category, MIME and extension), provider and parser brokers |
| `ContentType.Basic` | the provider over the enum's attributes; a parser that checks a file name against a declared MIME type |

## What Files keeps

- the category taxonomy, as input to the file-type registry's kinds (T7):

| Group | Categories |
|---|---|
| media | `Image` · `Video` · `Audio` · `Archive` |
| documents | `Document` · `Spreadsheet` · `Presentation` · `Ebook` · `CAD` · `GIS` |
| data and code | `Data` · `Database` · `SourceCode` · `Markup` · `Configuration` · `MarkupStyle` · `WebScript` · `Model3D` · `ProjectFile` |
| other | `Certificate` · `Log` · `Text` · `Email` · `Calendar` · `Contact` · `Backup` · `VirtualDisk` |

- the 43 type rows, as the registry's first seed (category · MIME · extension):

| Category | Rows |
|---|---|
| `Image` | `image/jpeg` jpg, jpeg · `image/png` png · `image/gif` gif · `image/svg+xml` svg · `image/webp` webp |
| `Video` | `video/mp4` mp4 · `video/x-msvideo` avi · `video/quicktime` mov · `video/webm` webm |
| `Audio` | `audio/mpeg` mp3 · `audio/wav` wav · `audio/aac` aac |
| `Archive` | `application/zip` zip · `application/x-rar-compressed` rar · `application/x-7z-compressed` 7z |
| `Document` | `application/pdf` pdf · `application/msword` doc · `application/vnd.openxmlformats-officedocument.wordprocessingml.document` docx |
| `Spreadsheet` | `application/vnd.ms-excel` xls · `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet` xlsx |
| `Presentation` | `application/vnd.ms-powerpoint` ppt |
| `Ebook` · `CAD` · `GIS` | `application/epub+zip` epub · `application/x-dwg` dwg · `application/x-shapefile` shp |
| `Data` · `Database` | `application/json` json · `application/xml` xml · `text/csv` csv · `application/x-sqlite3` sqlite |
| code and markup | `text/x-csharp` cs · `text/html` html · `text/yaml` yaml · `text/css` css · `text/javascript` js · `text/plain` sln |
| other | `application/x-x509-ca-cert` crt · `text/plain` log, txt · `message/rfc822` eml · `text/calendar` ics · `text/vcard` vcf · `application/octet-stream` bak · `application/x-vhd` vhd |

- the provider's lookups: by category, by MIME type, by extension, and a same-category check — the Formats registry's
  query surface.
- the name-against-declared-type check: match the extension, intersect with the declared MIME type's matches, reject
  on no overlap — the first step of the upload type check, before magic bytes.
- the stream parser seam (`ParseContentTypeAsync(stream, fileName?)`), which Formats fills with magic-byte sniffing.

## What Files drops

- the enum with attributes: a data registry (`file-types.json`, T7) replaces it, so a new type is a data row, not a
  release of the contract.
- `IStorageFile` with setters: the contract's file model is an immutable record keyed by `FileId`.
- the `Backbone.*` names, the `wow2.png` package icons and the empty solution.
