# WoW2.Sdk.Beta.Be.Files.Testing

The Files test kit:

- `FilesBrokerConformance` — the contract's suite: derive a test class, supply the brokers as two tenants reach them
  (and the clock when it is not the system's), and every rule of the contract runs against that implementation.
- `FilesSamples` — small byte samples with real magic bytes (PNG, JPEG, PDF) for upload tests.

```bash
dotnet add package WoW2.Sdk.Beta.Be.Files.Testing --prerelease
```
