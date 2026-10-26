# wow-two-sdk-beta.files — Architecture

*Last updated: 2026-10-26*

One contract, two registrations, one service that hosts the first.

```
product Domain / Application ──► Abstractions (contract)
product composition root ──► Module (Form 1)  or  Client (Forms 2/3)
service/Files.Api ──► Module (ProjectReference), + HTTP mirror of the contract + hosted pages (apps/web)
Client ──HTTP──► service/Files.Api ──► Module ──► the service's store
```

| Form | Registration | Store owner |
|---|---|---|
| 1 · library | `services.AddFiles()` + the module's migrations | the consumer |
| 2 · self-hosted | `services.AddFilesClient(…)` + a `files` container the consumer runs | the consumer's container |
| 3 · provided | `services.AddFilesClient(…)` | the central instance |

| Project | Responsibility |
|---|---|
| `packages/Abstractions` | the contract — broker, model, constants, errors; no DI, options or I/O |
| `packages/Module` | Form 1 over the module's store; embedded SQL migrations |
| `packages/Client` | Forms 2/3 over the service's HTTP API |
| `packages/Testing` | the fake and the conformance suite |
| `service/Files.Api` | hosting, the HTTP mirror of the contract, the hosted pages and console |
| `fe-files` | the TypeScript contract mirror, the embeddable widget, the forward to the hosted page |
| `apps/web` | hosted pages (`/greet`) and console (`/`), on `fe-files` through its exports |
