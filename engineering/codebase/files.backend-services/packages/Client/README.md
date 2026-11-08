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

// Or Identity tokens from the instance's trusted issuer
services.AddFilesClient(options =>
{
    options.BaseAddress = new Uri("https://files.example.com/");
    options.Identity = new FilesClientIdentityOptions
    {
        TokenEndpoint = new Uri("https://identity.example.com/connect/token"),
        ClientId = "orders",
        ClientSecret = configuration["Files:ClientSecret"],   // or ClientPrivateKey, for private_key_jwt
        // Resource defaults to BaseAddress with its trailing slash, the audience Files accepts
    };
});

// Work a person started that later runs as the service
using (FilesClientCalls.AsService())
    await store.DeleteAsync(id, cancellationToken);
```

- set `ApiKey` or `Identity`, not both; with neither, chain an own auth handler on the returned builder
- with `Identity`, a call exchanges the person's token (RFC 8693) when `IOAuth2SubjectTokenSource` finds one, by
  default the current request's bearer token, and sends the service's client-credentials token otherwise or under
  `FilesClientCalls.AsService()`; the be sdk handlers cache and renew both, the Files client keeps no token
- handlers and the primary handler set on the returned builder serve every call, token calls included

- uploads stream as multipart; downloads stream, one byte range at a time
- a failure comes back as the service's own `AppError` type (`FileNotFound`, `Validation`, …); an unreachable service
  as `ExternalUnavailable`
