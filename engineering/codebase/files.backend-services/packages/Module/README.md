# WoW2.Sdk.Beta.Be.Files.Module

Form 1 of the Files capability: uploads and the file store in the consumer's process, over the consumer's database
and blob store. The store is PostgreSQL (`Provider`, default `Postgres`; `Sqlite` for tests). The schema ships embedded
once per engine, `Migrations/{Postgres,Sqlite}/NNN-name/{Apply,Rollback}.sql`, as the be sdk migration set `files`
(history `files_migration_history`): `AddFilesMigrations()` registers the set of the module's engine over its
connection string, and the be sdk boot or deploy helpers apply it with the consumer's own sets.

- uploads spool through a temp file, are hashed and checked against their name (`Files.Formats`) and the app policy,
  then stored once per tenant at `{tenant}/{sha256[..2]}/{sha256}`
- a retried upload with the same idempotency key returns the first file
- reads whole or by byte range; lists newest first with a cursor; rename within the stored type; soft delete,
  restore within retention; an hourly sweep purges deleted, expired and unused bytes
- URLs: signed links to private files and immutable links to public ones on the content host (`Delivery.BaseUrl`,
  `Delivery.SigningKey`); `IFilesDeliveryService` is what the Server routes serve through
- references: a file lives while a record uses it; per app, unreferenced files purge after `OrphanRetention` and
  uploads expire after `DefaultLifetime`
- every call acts for `IFilesCallerService.Current`, read from the kernel `ICurrentPrincipal`: the tenant its issuer or
  key stamped (`Partition`), else the be sdk ambient tenant or `DefaultTenantId`, plus its subject, issuer, organization,
  app and actor; replace the service to resolve callers another way

```bash
dotnet add package WoW2.Sdk.Beta.Be.Files.Module --prerelease
```

```csharp
services.AddLocalBlobStorage(Path.Combine(env.ContentRootPath, "data", "blobs")); // or AddS3BlobStorage(…)
services.AddFiles().Configure(options =>
{
    options.ConnectionString = "Host=localhost;Database=app;Username=app;Password=…"; // Provider defaults to Postgres
    options.Policies["avatars"] = new() { MaxBytes = 5 * 1024 * 1024 };
    options.Policies["avatars"].AllowedKinds.Add(FileKind.Image);
});
services.AddFilesMigrations(migrations => { /* version */ });
// at boot or in the deploy step: await app.Services.MigrateBespokeOnStartupAsync(); // or MigrateBespokeForDeployAsync()
```
