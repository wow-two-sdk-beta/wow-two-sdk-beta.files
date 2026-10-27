# wow-two-sdk-beta.files — Package registry

*Last updated: 2026-10-26*

> Every package this repo publishes, one version line per registry. Layer rules:
> wow-two-ws/conventions/development/repo/structure/domain-structure.md § Layers. Shape and options weighed:
> [T6](../research/files-analysis-2026-10/t6-library-shape.md).

- a package's project lands with its first code, so no empty package is ever packed; `planned` rows have no project.
- namespaces root at `WoW.Two.Sdk.Beta.Be.Files`; code moved from core takes `Files.Images` and `Files.Documents`.

| Package | Layer | References | Heavy deps | Forms | State | Consumers |
|---|---|---|---|---|---|---|
| `WoW2.Sdk.Beta.Be.Files.Abstractions` | contract | the kernel `WoW2.Sdk.Beta.Be.Abstractions` only; no framework reference | none | all | contract skeleton | product Domain and Application projects |
| `WoW2.Sdk.Beta.Be.Files.Formats` | edge | Abstractions | none | all | types, detection, mismatch check, safe names | Module, upload checks in any product, `fe-files/icons` (by its registry) |
| `WoW2.Sdk.Beta.Be.Files.Module` | forms | Abstractions, Formats, core | none | 1 | uploads, store, sweep; template sample | products on Form 1, `service/Files.Api` |
| `WoW2.Sdk.Beta.Be.Files.Client` | forms | Abstractions, core | none | 2, 3 | template sample | products on an owner instance or the shared host |
| `WoW2.Sdk.Beta.Be.Files.Server` | extension | Module, core | tusdotnet, once resumable uploads land | 1, and the service | project only | Form 1 hosts, `service/Files.Api` |
| `WoW2.Sdk.Beta.Be.Files.Testing` | test kit | Abstractions, `WoW2.Sdk.Beta.Be.Testing` | xunit (via be Testing) | all | conformance: uploads, store; template sample | test projects |
| `WoW2.Sdk.Beta.Be.Files.Compression` | edge | Abstractions | SharpCompress, ZstdSharp (unverified) | all | planned: streamed ZIP | Module (shares, exports), any product alone |
| `WoW2.Sdk.Beta.Be.Files.Images` | edge | Abstractions | SkiaSharp | all | planned: moves from core `Media/Images` | Module (variants), products that edit images |
| `WoW2.Sdk.Beta.Be.Files.Documents` | edge | Abstractions | PDFsharp, PdfPig; PDFium later | all | planned: moves from core `Media/Pdf` | Module (previews), products with PDF tools |
| `WoW2.Sdk.Beta.Be.Files.Video` | extension | Abstractions, core | the FFmpeg binary | 1, and the service | planned: P2 | Module (posters, probes) through a contract port |
| `WoW2.Sdk.Beta.Be.Files.Scanning` | extension | Abstractions, core | none (ClamAV over TCP) | 1, and the service | planned: P2 | Module through a contract port |
| `@wow-two-beta/fe-files` | fe | peers `@wow-two-beta/fe` `>=0.0.n <0.1.0`, `vue` | none | all | icons; template sample | product frontends, `apps/web` |

- `fe-files` subpaths: `/contracts` (the TypeScript mirror), `/kit` (upload kit, previews, share UI), `/icons` (the
  file icon pack, usable without a Files instance).
- core keeps `Storage/` (byte adapters, signed-URL issuers) and `Web/Compression`; Files builds on them.
