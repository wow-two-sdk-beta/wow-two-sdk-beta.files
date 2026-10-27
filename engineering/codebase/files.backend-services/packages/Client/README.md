# WoW2.Sdk.Beta.Be.Files.Client

Forms 2 and 3 of the Files capability: every Files broker (`IFileUploadsBroker`, `IFileStoreBroker`,
`IFileReferencesBroker`, `IFileUrlsBroker`, `IFileAuditBroker`) over the running Files service's HTTP API. The product
changes form by configuration: swap `AddFiles()` for `AddFilesClient(…)` in its composition root; no other code
changes, and the same conformance suite passes on both.

```bash
dotnet add package WoW2.Sdk.Beta.Be.Files.Client --prerelease
```

```csharp
// A tenant API key
services.AddFilesClient(options =>
{
    options.BaseAddress = new Uri("https://files.example.com/");
    options.ApiKey = configuration["Files:ApiKey"];
});

// Or a client-credentials token from the instance's trusted issuer
services.AddFilesClient(options => options.BaseAddress = new Uri("https://files.example.com/"))
    .AddOAuth2ClientCredentials(token => { /* token endpoint, client id and secret, scope "files.read files.write" */ });
```

- uploads stream as multipart; downloads stream, one byte range at a time
- a failure comes back as the service's own `AppError` type (`FileNotFound`, `Validation`, …); an unreachable service
  as `ExternalUnavailable`
