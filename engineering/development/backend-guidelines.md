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
- be sdk `10.0.82+` applies boot migrations on local runs only; `Files.Api` sets `ApplyWhenDeployed = true` until its
  deploy runs a migrate step.
- macOS file systems ignore case and Linux CI does not: match on-disk casing in paths and globs, and never assert a
  case-only miss in a test.
