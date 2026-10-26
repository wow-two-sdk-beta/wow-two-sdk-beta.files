# WoW2.Sdk.Beta.Be.Files.Client

Forms 2 and 3 of the Files capability: `IGreetingsBroker` over the running Files service's HTTP API. The product
changes form by configuration: swap `AddFiles()` for `AddFilesClient(…)` in its composition root; no other code
changes.

```bash
dotnet add package WoW2.Sdk.Beta.Be.Files.Client --prerelease
```

```csharp
services.AddFilesClient(options => options.BaseAddress = new Uri("https://files.example.com/"));
```
