# wow-two-sdk-beta.files

Files for every product — store, upload, transform, compress, share and serve them — as a .NET package family, an npm package and a service image, under one contract.

## Artifacts

| Artifact | Registry | Line · tag | Install |
|---|---|---|---|
| `WoW2.Sdk.Beta.Be.Files.Abstractions` — the contract | nuget.org | `10.y.z-beta` · `be-v*` | `dotnet add package WoW2.Sdk.Beta.Be.Files.Abstractions --prerelease` |
| `WoW2.Sdk.Beta.Be.Files.Module` — Form 1, in process | nuget.org | `10.y.z-beta` · `be-v*` | `dotnet add package WoW2.Sdk.Beta.Be.Files.Module --prerelease` |
| `WoW2.Sdk.Beta.Be.Files.Client` — Forms 2 and 3, over HTTP | nuget.org | `10.y.z-beta` · `be-v*` | `dotnet add package WoW2.Sdk.Beta.Be.Files.Client --prerelease` |
| `WoW2.Sdk.Beta.Be.Files.Testing` — fake and conformance suite | nuget.org | `10.y.z-beta` · `be-v*` | `dotnet add package WoW2.Sdk.Beta.Be.Files.Testing --prerelease` |
| `@wow-two-beta/fe-files` — contract mirror, widget, forward | npm | `0.0.z` · `fe-v*` | `pnpm add @wow-two-beta/fe-files` |
| `files` service image | GHCR | `X.Y.Z` · `v*` | `ghcr.io/wow-two-sdk-beta/wow-two-sdk-beta.files/files`, deployed by Wheelhouse |

A product pins the family with one `FilesSdkVersion` property beside `BackendSdkVersion`.

## Forms

```csharp
// Form 1 — in process, over the product's own database (it applies the module's migrations)
services.AddFiles().Configure(options => options.ConnectionString = "...");

// Forms 2 and 3 — over the running Files service; the product's code does not change
services.AddFilesClient(options => options.BaseAddress = new Uri("https://files.example.com/"));
```

## Layout

```
product/                 ← the capability: spec, state, features, flows, brand (no code)
engineering/             ← the how: architecture (contract, package registry), planning, deployment, scripts
└── codebase/
    ├── files.backend-services/    ← packages/ (the NuGet family) · service/Files.Api · tests/
    └── files.frontend-services/   ← packages/fe-files (npm) · apps/web (hosted pages + console)
```

## Run it (dev)

```bash
# Service (the https launch profile sets LOCAL_SETTINGS=true, so an optional appsettings.Local.json applies)
cd engineering/codebase/files.backend-services && dotnet run --project service/Files.Api --launch-profile https

# Pages and console, proxied to the service
cd engineering/codebase/files.frontend-services && pnpm install --frozen-lockfile && pnpm dev
```

## Test it

```bash
# Backend: unit, integration, E2E, conformance and migration tiers (SQLite, no Docker); the build also builds the
# frontend workspace, so Node and pnpm are required
cd engineering/codebase/files.backend-services && dotnet test Files.BackendServices.slnx

# Frontend: lint, tests (builds fe-files first), production build, packed-consumer check
cd engineering/codebase/files.frontend-services && pnpm lint && pnpm test && pnpm build && pnpm check:package

# Release scripts: the tag version logic
bash engineering/scripts/test-next-version.sh
```

## Pointers

- Capability spec: `product/product.md` · state and decisions: `product/context.md`
- Contract: `engineering/architecture/contract.md` · packages: `engineering/architecture/package-registry.md`
- Releases: `engineering/scripts/scripts.md` · deployment: `engineering/deployment/deployment.md`
- Plan: `engineering/planning/backlog.md` · active version: newest `engineering/planning/version-track/v{X.Y}/`
- Conventions: `wow-two-ws/conventions/development/repo/structure/domain-structure.md`
