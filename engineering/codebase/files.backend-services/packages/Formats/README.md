# WoW2.Sdk.Beta.Be.Files.Formats

The Files file-type registry: what a file is, from its name and its bytes.

- one registry, `Registry/file-types.json`: extension, MIME type, kind, label, magic bytes, previewer, risk
- lookups by extension, MIME type and kind; a name-against-declared-type check
- magic-byte sniffing, so a renamed file cannot pass as another type
- safe file names: Unicode normalization, length cap, no path parts

No store and no core dependency: a product takes it alone for upload checks.

```bash
dotnet add package WoW2.Sdk.Beta.Be.Files.Formats --prerelease
```

Source: https://github.com/wow-two-sdk-beta/wow-two-sdk-beta.files
