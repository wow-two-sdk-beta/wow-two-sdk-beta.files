# File References

> **Lookup table only.** Do NOT read files proactively — only when the current task requires them.
> **Tracks docs outside `engineering/codebase/`.** Source files are navigated via `tree`/`find`/`grep`, never listed here.

| Question about | Read |
|---|---|
| The capability — problem, forms, model, phases | `product/product.md` |
| Name and one-line description (read by consoles) | `product/product.json` |
| State, tasks, decisions | `product/context.md` |
| Feature list + state | `engineering/planning/backlog.md` § Features |
| One feature's spec | `product/features/{feature}.md` |
| User / product flows | `product/flows/flows.md` |
| Hosted pages' brand | `product/brand/brand.md` |
| Technical overview · stack · map | `engineering/engineering.md` |
| System design — forms map, package graph, service role | `engineering/architecture/architecture.md` |
| The broker contract, every form | `engineering/architecture/contract.md` |
| Packages, layers, consumers | `engineering/architecture/package-registry.md` |
| Architecture decisions | `engineering/architecture/decisions/decisions.md` |
| What lives where in the code | `engineering/codebase/codebase.md` |
| Backend dev guidelines | `engineering/development/backend-guidelines.md` |
| Frontend dev guidelines | `engineering/development/frontend-guidelines.md` |
| Iteration / version workflow | `engineering/development/iteration-guide.md` |
| Backlog — every unbuilt item | `engineering/planning/backlog.md` |
| Active version | newest `engineering/planning/version-track/v{X.Y}/v{X.Y}.md` |
| Working rules for agents | `engineering/development/rules.md` |
| Deploy / ops | `engineering/deployment/deployment.md` |
| What deploys — service, sites, settings, dev check | `engineering/deployment/deploy.yml` |
| Releases — versions, tags, verification | `engineering/scripts/scripts.md` |
| Research and analysis batches | `engineering/research/research.md` |
| The pre-build analysis and its decision queue | `engineering/research/files-analysis-2026-10/files-analysis-2026-10.md` |
| Prompts for the Identity and Wheelhouse chats | `engineering/research/files-analysis-2026-10/prompt-*.md` |
| What the 2024 storage.file repo held | `engineering/research/legacy-storage-file.md` |

## Source projects (`engineering/codebase/`)

> Individual files NOT listed — use `tree`/`find`/`grep`. Describe projects only.

### `codebase/files.backend-services/`
| Project | What it is |
|---|---|
| `packages/Abstractions` | the contract: `IFile*Broker`s, models, constants, scopes, errors; the template's `IGreetingsBroker` until v0.1 removes it |
| `packages/Formats` | the file-type registry (`Registry/file-types.json`), `FileTypeMapper`, `FileContentValidator`, `FileNameMapper` |
| `packages/Module` | Form 1: uploads, store, references and sweep over SQLite + core `IBlobRepository`, `AddFiles()`, callers, policies, embedded SQL migrations |
| `packages/Client` | Forms 2/3: the broker over the service's HTTP API, `AddFilesClient()` |
| `packages/Server` | `MapFilesDelivery()`: signed `/f` and public `/m` routes; later share and request routes, upload protocols, admin API |
| `packages/Testing` | the conformance suites (Files brokers, the Greetings sample) and byte samples |
| `service/Files.Api` | the deployable host: the module, the HTTP mirror, the hosted pages |
| `tests/Files.Tests.{Unit,Integration,E2E,Conformance,Migrations}` | the test tiers |

### `codebase/files.frontend-services/`
| Project | What it is |
|---|---|
| `packages/fe-files` | `@wow-two-beta/fe-files`: contract mirror, `/icons` (generated type table, `FileIcon`), `GreetingWidget` |
| `apps/web` | the console (`/`), the icon page (`/icons`) and hosted pages (`/greet`), served by `Files.Api` |
