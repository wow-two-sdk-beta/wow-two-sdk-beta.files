# wow-two-sdk-beta.files

Files for every product — store, upload, transform, compress, share and serve them — as a .NET package family, an npm package and a service image, under one contract.

## Lazy loading

- Do NOT pre-read files at conversation start; open a file only when the task needs it.
- `.claude/rules/file-references.md` is a lookup table, not a reading list.
- Navigate code under `engineering/codebase/` with `tree`/`find`/`grep` — it is not listed in references.

## Structure

```
product/                 ← the capability (no code): product · context · features · flows · brand
engineering/             ← the HOW
├── architecture/        ← architecture · contract · package-registry · decisions/ · flows/
├── development/         ← this repo's dev guidelines (defer to wow-two-ws/conventions/)
├── planning/            ← backlog.md + version-track/v{X.Y}/ (newest = active; drives the image version only)
├── deployment/          ← deploy.yml · Dockerfile · docker-compose.yml · deployment.md
├── research/ · scripts/ ← analyses · release helpers publish-packages.yml calls
└── codebase/            ← THE CODE — only place code lives
    ├── files.backend-services/    ← Files.BackendServices.slnx: packages/{Abstractions,Formats,Module,Client,Server,Testing} · service/Files.Api · tests/Files.Tests.*
    └── files.frontend-services/   ← pnpm workspace: packages/fe-files (npm) · apps/web (hosted pages + console)
```

## Rules

- Shape: wow-two-ws/conventions/development/repo/structure/domain-structure.md; repo-specific deltas only in `engineering/development/`.
- The contract lives in `packages/Abstractions`; Module (Form 1) and Client (Forms 2/3) implement it, and
  `Files.Tests.Conformance` runs the one suite (in `packages/Testing`) against both. The template's Greetings sample
  stays until v0.1 iteration 6 removes it.
- Core keeps the `Storage/` byte adapters; every Files byte goes through core `IBlobRepository`.
- Code moved from core `Media/Images` and `Media/Pdf` takes the `Files.Images` and `Files.Documents` namespaces.
- A package project lands with its first code; planned packages are rows in `engineering/architecture/package-registry.md`.
- A new file type is a row in `packages/Formats/Registry/file-types.json`, the one source for be tables and fe icons.
- Identity stays behind its contracts (`engineering/research/files-analysis-2026-10/t8-identity-contracts.md`) until
  the final integration; references run one way, Files → Identity contracts only.
- A package references only what its layer row allows; the service takes the packages by `ProjectReference` and adds
  only hosting, the HTTP mirror of the contract and the hosted pages.
- Package versions come from tags (`be-v*` NuGet, `fe-v*` npm) in `publish-packages.yml`; never commit a version,
  never create a GitHub Release for a package tag. The image version comes from the version track (`v*`).
- Core is pinned (`BackendSdkVersion`, the exact `@wow-two-beta/fe`); a needed core change goes to core first, then the
  pin moves here. `core-latest.yml` tests the newest core nightly; a red run is a fix here, not a gate.
- Schema changes are `packages/Module/Migrations/NNN-name/{Apply,Rollback}.sql` pairs; never edit an applied one.
- The backend build runs the frontend build (`BuildSpa`), so it needs Node and pnpm; no test tier needs Docker.
