# wow-two-sdk-beta.files — Backend Development Guidelines

Defer to wow-two-ws/conventions/development/backend/ and the domain repo shape in
wow-two-ws/conventions/development/repo/structure/domain-structure.md. Repo deltas:

- `packages/*` are the NuGet family: packable, XML docs, warnings as errors, a `README.md` beside each csproj (all set
  by path in `Directory.Build.props`). `service/` and `tests/` are not packable.
- A new package is a `packages/{Layer}/WoW.Two.Sdk.Beta.Be.Files.{Layer}.csproj`; its id drops `.Two` (`WoW2.…`), its
  namespaces root at `WoW.Two.Sdk.Beta.Be.Files`, and its row joins `engineering/architecture/package-registry.md`.
- The contract (`Abstractions`) holds no DI registration, options binding or I/O; a new broker member gets a
  conformance case in `packages/Testing` that every implementation passes.
- Store: PostgreSQL 18 through `DatabaseSettings:ConnectionString` (`DB_CONNECTION` wins); `DatabaseSettings:Provider`
  (`FilesModuleOptions.Provider`) defaults to `Postgres` and takes `Sqlite` only for tests and a local run's
  `appsettings.Local.json`. Tables and enum types carry the `files_` prefix, since a Form 1 consumer applies them beside
  its own. The schema is the migration set `files` (`AddFilesMigrations`, history `files_migration_history`), written
  once per engine: a schema change is a pair in both `packages/Module/Migrations/Postgres/NNN-name/` and
  `Migrations/Sqlite/NNN-name/` under one ordinal, and `FilesDbContext` maps both shapes; never edit an applied one.
- PostgreSQL shape (wow-two-ws `persistence/database/postgres/postgres.md`): `uuid` ids (the entities keep lowercase
  strings, converted in the context), `timestamptz` instants named `*_at` (`*_at_utc` on SQLite), native enums with
  snake_case labels (`files_file_kind`, …; SQLite stores the member names), `varchar(n)` where the contract sets a
  limit, `text` where callers set none. A new enum member is an `ALTER TYPE … ADD VALUE` migration; the Migrations tier
  compares the labels with the CLR enums.
- Bytes: the host registers core `IBlobRepository`; `Files.Api` uses a local folder at `Storage:Root` (default
  `data/blobs`). Module settings bind from the `Files` section: `DefaultTenantId`, `Policies` by app, `SweepInterval`,
  `Delivery:BaseUrl` and the secret `Delivery:SigningKey` (`Files__Delivery__SigningKey`). Callers: `Files:Auth` trusted
  issuers and API keys (SHA-256 only), read through Identity's `Auth` package; a new action names its policy, the scope
  (`FilesScopeConstants`), which also asks a person for the matching `FilesPermissionConstants` permission.
- `dotnet test Files.BackendServices.slnx` runs every tier; the backend build needs Node and pnpm for the pages. The
  Migrations, Integration and module Conformance tiers run once per lane (`tests/Shared/Lanes`): SQLite in process, and
  PostgreSQL in one Testcontainers container per assembly with a database per store. Without Docker the PostgreSQL lane
  reports its tests skipped; `FILES_TESTS_POSTGRES=off` skips it on purpose. A PostgreSQL-lane class joins
  `[Collection(PostgresLane.Collection)]`.

## Traps

- The be sdk secure-headers preset rewrites CSP, COEP and CORP as the response starts, over what an endpoint set. An
  endpoint answers its own with `.OverrideSecureHeader(name, value)` (null removes); the delivery routes do.
- `UseApiDefaults` serves wwwroot before routing, so a static file has no endpoint to mark and keeps CORP
  `same-origin`; a folder other origins embed opts in through `CrossOriginEmbeddingPaths` (`/icons` does).
- MVC's form value providers read a multipart body before a streaming `MultipartReader` action runs; such an action
  carries `[DisableFormValueBinding]`.
- A class-level policy and an action-level policy combine as AND; every action names its own scope policy.
- `WebApiTestHost.Clock` starts at 2000-01-01, while JwtBearer checks lifetimes on the real clock: test tokens are
  minted on `DateTime.UtcNow`, app times read the fake clock.
- Settings read at registration miss the test host's overrides; read them lazily (`Configure<T>` callbacks, factories).
- be sdk `10.0.82+` applies boot migrations on local runs only; `Files.Api` sets `ApplyWhenDeployed = true`, since the
  image's `migrate` verb is the file mover and the descriptor carries no migrate step.
- Deployed dev runs as .NET Development, so `appsettings.Development.json` never chooses the engine: a local SQLite
  store is `appsettings.Local.json`'s `DatabaseSettings:Provider: Sqlite`.
- One Npgsql data source per connection string loads the enum types on its first connection; a database whose types
  were dropped and recreated under a live data source needs a new one, so the PostgreSQL lane gives every store its
  own database rather than resetting one.
- macOS file systems ignore case and Linux CI does not: match on-disk casing in paths and globs, and never assert a
  case-only miss in a test.
