# WoW2.Sdk.Beta.Be.Files.Formats

The Files file-type registry: what a file is, from its name and its bytes.

- one registry, `Registry/file-types.json`: extension, MIME type, kind, label, magic bytes, previewer, risk
- `FileTypeMapper`: lookups by extension, name and MIME type; detection from leading bytes
- `FileContentValidator`: rejects bytes that contradict the claimed type, so a renamed file cannot pass as another
- `FileNameMapper`: safe names (NFC, no path parts or control characters, length cap) and `Content-Disposition`

No store and no core dependency: a product takes it alone for upload checks.

```csharp
var file = FileContentValidator.Default.Validate(upload.FileName, upload.ContentType, head, complete);
// Ok: file.Name (safe), file.Type.MimeType, file.Type.Kind · Fail: NameInvalid or ContentMismatch
```

```bash
dotnet add package WoW2.Sdk.Beta.Be.Files.Formats --prerelease
```

Source: https://github.com/wow-two-sdk-beta/wow-two-sdk-beta.files
