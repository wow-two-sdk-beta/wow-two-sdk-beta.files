# WoW2.Sdk.Beta.Be.Files.Abstractions

The Files contract: the brokers `IFileUploadsBroker`, `IFileStoreBroker`, `IFileReferencesBroker`, `IFileUrlsBroker`
and `IFileAuditBroker`, their models (`FileModel`, `FileId`, `UploadSpec`, `SignedFileUrl`, …), `FilesConstants`,
`FilesRouteConstants`, `FilesScopeConstants` and the `FilesErrors` catalogue. A product's Domain and Application
projects reference this package only; its composition root picks an implementation:

- `WoW2.Sdk.Beta.Be.Files.Module` — Form 1, in process, over the product's own database and blob store.
- `WoW2.Sdk.Beta.Be.Files.Client` — Forms 2 and 3, over the running Files service.

```bash
dotnet add package WoW2.Sdk.Beta.Be.Files.Abstractions --prerelease
```
