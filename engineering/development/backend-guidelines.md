# wow-two-sdk-beta.files — Backend Development Guidelines

Defer to wow-two-ws/conventions/development/backend/ and the domain repo shape in
wow-two-ws/conventions/development/repo/structure/domain-structure.md. Repo deltas:

- `packages/*` are the NuGet family: packable, XML docs, warnings as errors, a `README.md` beside each csproj (all set
  by path in `Directory.Build.props`). `service/` and `tests/` are not packable.
- A new package is a `packages/{Layer}/WoW.Two.Sdk.Beta.Be.Files.{Layer}.csproj`; its id drops `.Two` (`WoW2.…`), its
  namespaces root at `WoW.Two.Sdk.Beta.Be.Files`, and its row joins `engineering/architecture/package-registry.md`.
- The contract (`Abstractions`) holds no DI registration, options binding or I/O; a new broker member gets a
  conformance case in `packages/Testing` that every implementation passes.
- Store: SQLite through `DatabaseSettings:ConnectionString` (`DB_CONNECTION` wins). Tables carry the `files_` prefix,
  since a Form 1 consumer applies them beside its own. A new table is a new
  `packages/Module/Migrations/NNN-name/{Apply,Rollback}.sql` pair; never edit an applied one.
- Bytes: the host registers core `IBlobRepository`; `Files.Api` uses a local folder at `Storage:Root` (default
  `data/blobs`). Module settings bind from the `Files` section: `DefaultTenantId`, `Policies` by app, `SweepInterval`,
  `Delivery:BaseUrl` and the secret `Delivery:SigningKey` (`Files__Delivery__SigningKey`). Callers: `Files:Auth` API keys
  (SHA-256 only) and trusted issuers; a new action names its scope policy (`FilesScopeConstants`).
- `dotnet test Files.BackendServices.slnx` runs every tier; the backend build needs Node and pnpm for the pages.
