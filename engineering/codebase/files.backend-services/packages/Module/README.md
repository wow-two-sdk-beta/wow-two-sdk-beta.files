# WoW2.Sdk.Beta.Be.Files.Module

Form 1 of the Files capability: uploads and the file store in the consumer's process, over the consumer's database
and blob store. The consumer owns the tables and applies the embedded `Migrations/NNN-name/{Apply,Rollback}.sql` with
the be sdk migrator.

- uploads spool through a temp file, are hashed and checked against their name (`Files.Formats`) and the app policy,
  then stored once per tenant at `{tenant}/{sha256[..2]}/{sha256}`
- a retried upload with the same idempotency key returns the first file
- reads whole or by byte range; lists newest first with a cursor; rename within the stored type; soft delete,
  restore within retention; an hourly sweep purges deleted, expired and unused bytes
- every call acts for `IFilesCallerService.Current`: the be sdk ambient tenant or `DefaultTenantId`, and the user's
  `sub`, `org_id` and `azp` claims; replace the service to resolve callers another way

```bash
dotnet add package WoW2.Sdk.Beta.Be.Files.Module --prerelease
```

```csharp
services.AddLocalBlobStorage(Path.Combine(env.ContentRootPath, "data", "blobs")); // or AddS3BlobStorage(…)
services.AddFiles().Configure(options =>
{
    options.ConnectionString = "Data Source=app.db";
    options.Policies["avatars"] = new() { MaxBytes = 5 * 1024 * 1024 };
    options.Policies["avatars"].AllowedKinds.Add(FileKind.Image);
});
services.AddDatabaseBespokeMigrations(typeof(FilesModuleAssembly).Assembly, migrations => { /* provider, version */ });
```
