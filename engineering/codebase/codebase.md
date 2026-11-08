# codebase/

The code lives in `files.backend-services/` (.NET) and `files.frontend-services/` (Vue).

## Backend

- `Files.BackendServices.slnx` groups `Packages/`, `Services/` and `Tests/`.
- `packages/` holds the NuGet family `WoW2.Sdk.Beta.Be.Files.*`: `Abstractions` (the contract), `Formats` (the
  file-type registry, `Registry/file-types.json`), `Module` (Form 1, with the migration set `files` once per engine,
  `Migrations/{Postgres,Sqlite}/NNN-name/{Apply,Rollback}.sql`),
  `Client` (Forms 2/3), `Server` (delivery and share routes) and `Testing` (fake + conformance suite). Planned packages
  get a project with their first code: `engineering/architecture/package-registry.md`.
  `Directory.Build.props` makes them packable by path; the core pin in `Directory.Packages.props` is their floor.
- `service/Files.Api` hosts the module by `ProjectReference`: `AddApiDefaults()` / `UseApiDefaults()`, a local blob
  folder (`Storage:Root`), the store on PostgreSQL (`DatabaseSettings:Provider`, `Sqlite` for tests) with the module's
  set applied at boot (`AddFilesMigrations`), the delivery routes (`MapFilesDelivery()`), the API
  (`Files/Controllers`: files, audit, the HTTP mirror of the contract) behind `Auth/` (API keys, trusted issuers'
  tokens, one policy per scope), `SystemController`, and the pages in
  `wwwroot/` (filled by the `BuildSpa` target; `-p:BuildSpa=false` skips it).
- Tests: `Unit` (package logic), `Integration` (the module over a throwaway store), `E2E` (the host in process over
  SQLite: wire, pages, `/health`, the descriptor's dev check), `Conformance` (one suite against module and client),
  `Migrations` (the module's SQL per engine). `Integration`, `Migrations` and the module's `Conformance` run once per
  lane (`tests/Shared/Lanes`): SQLite, then PostgreSQL in Testcontainers, skipped without Docker, so none needs Docker.
  Run them with `dotnet test Files.BackendServices.slnx`.

## Frontend

A pnpm workspace: `packages/fe-files` (`@wow-two-beta/fe-files`, published: `./contracts`, `./icons`, `./kit`) and
`apps/web` (console at `/`, icon page at `/icons`, hosted pages; private). `fe-files/src/icons/FileTypeTable.ts` is
generated from the be registry by `pnpm gen:file-types`. `apps/web/vite.icons.ts` writes every icon file
(`icons/{ext}.svg`, `icons/kinds/{kind}.svg`) into the bundle and serves them in dev; `Files.Api/Icons` answers the
misses. `scripts/deploy.mjs` copies `apps/web/dist/` to `files.backend-services/service/Files.Api/wwwroot/`.

Rules: wow-two-ws/conventions/development/repo/structure/domain-structure.md
