# wow-two-sdk-beta.files — Contract

*Last updated: 2026-10-26*

> One broker set, the same in every form: a product moves between Form 1, an owner instance and the shared files host
> by configuration only. Rules: wow-two-ws/conventions/development/capability-forms.md § Contract. Design evidence:
> [T4](../research/files-analysis-2026-10/t4-contract.md).

- Types live in `packages/Abstractions`, namespace root `WoW.Two.Sdk.Beta.Be.Files`.
- Brokers carry the `Broker` suffix per the broker construct (T4 named them `IFileUploads` and so on).
- The file model is `FileModel`, not T4's `FileInfo`, which collides with `System.IO.FileInfo` under implicit usings.
- Every call acts for the current principal and sees only its tenant's files; results use the kernel `Result` and
  `AppError`; HTTP status mapping stays product-side.

## Identifiers

| Id | Shape | Notes |
|---|---|---|
| `FileId` | UUIDv7 | assigned by the instance; survives every tenant move |
| `TenantId` · `OrganizationId` · `AppId` | string | from the tenant registry and token claims |
| storage key | `{tenant}/{sha256[..2]}/{sha256}` | variants `{tenant}/v/{sha256}/{variant}.{ext}` |
| `UploadId` | UUIDv7 | valid until the upload session expires |
| share token | 22+ URL-safe characters | stored hashed |

## Brokers

| Broker | Calls | State |
|---|---|---|
| `IFileUploadsBroker` | `UploadAsync(stream, spec)` | Module; Client in v0.1 |
| `IFileStoreBroker` | `GetAsync` · `OpenReadAsync(id, range?)` · `ListAsync(query)` · `UpdateAsync(patch)` · `DeleteAsync` · `RestoreAsync` | Module; Client in v0.1 |
| `IFileReferencesBroker` | `AttachAsync(id, ref)` · `DetachAsync` · `ListAsync(id)` | declared; v0.1 |
| `IFileUrlsBroker` | `IssueAsync(id, spec)` → signed URL · `GetPublicUrlAsync(id, variant?)` | declared; v0.1 |
| `IFileUploadsBroker` tickets | `BeginAsync(spec)` → ticket · `CompleteAsync(uploadId)` · `CancelAsync` | planned |
| `IFileSharesBroker` | `CreateAsync(spec with gates)` · `UpdateAsync` · `RevokeAsync` · `ListAsync` · `AccessLogAsync` | planned |
| `IFileTransformsBroker` | `GetVariantAsync(id, name)` · `RequestAsync(spec)` → job | planned |
| `IFileArchivesBroker` | `ZipAsync(ids, spec)` → job · `StreamZipAsync(ids, output)` · `ExtractAsync(id, folder)` | planned |
| `IFileJobsBroker` | `GetAsync(jobId)` · `WaitAsync` | planned |
| `IFileAdminBroker` | usage, quotas, policies, audit, tenants | planned |

- models today: `FileModel`, `FileQuery`, `FilePage`, `FilePatch`, `FileContent`, `ByteRange`, `UploadSpec`,
  `FileReference`, `UrlSpec`, `SignedFileUrl`; enums `FileKind`, `FileState`, `FileVisibility`, `FileDisposition`.
- `FileState`: `Uploading` → `Pending` → `Ready`, or `Rejected` · `Quarantined`; a soft delete sets `DeletedAtUtc`.
- the stored content type is the registry's for the recognized type, never the uploader's; a rename may change the
  extension only to one of the same content type.
- an expired file, or a deleted one past its app's retention, is gone for every call before the sweep purges it.
- share gates, once declared, are a closed set: expiry, password, max downloads, sign-in, email; later IP and terms.

## HTTP API

Base `/api/files/v1` (`FilesRouteConstants.Api`); the service mirrors the brokers.

| Method and path | Broker call | Scope |
|---|---|---|
| `POST /files` (multipart) | `UploadAsync` | `files.write` |
| `GET /files` · `GET /files/{id}` | `ListAsync` · `GetAsync` | `files.read` |
| `GET /files/{id}/content` | `OpenReadAsync` | `files.read` |
| `PATCH /files/{id}` · `DELETE /files/{id}` · `POST /files/{id}/restore` | update · delete · restore | `files.write` |
| `PUT·DELETE /files/{id}/references/{ref}` · `GET /files/{id}/references` | references | `files.write` · `files.read` |
| `POST /files/{id}/urls` · `GET /files/{id}/public-url` | `IssueAsync` · `GetPublicUrlAsync` | `files.read` |
| `POST /uploads` · `POST /uploads/{id}/complete` · `DELETE /uploads/{id}` | tickets | planned |
| `POST /shares` · `GET /shares` · `PATCH·DELETE /shares/{id}` | shares | `files.share`; planned |
| `GET /usage` · `GET /audit` · `PUT /policies/{app}` | admin | `files.admin`; planned |
| `GET·POST·PATCH /tenants` · `POST /tenants/{id}/keys` · `POST /tenants/{id}/export` | shared host operator | `files.tenants`; planned |
| `POST /imports` · `GET /imports/{id}` | migration | `files.admin`; planned |

- scopes: `FilesScopeConstants` (`files.read`, `files.write`, `files.share`, `files.admin`, `files.delegate`,
  `files.tenants`); Identity issues them (T8 C3).
- `Idempotency-Key` on every creating `POST`; cursor paging; filters `app`, `owner`, `kind`, `state`, `createdAfter`.
- the Client implements the brokers by hand over the be sdk HTTP client; OpenAPI comes from the host.

## Hosting routes

Served by `packages/Server` on the content host (`files.{domain}` or the shared host), never on an app's origin.

| Route | Serves | Auth |
|---|---|---|
| `GET /f/{fileId}[/{variant}]?exp=&sig=` | a private file through a signed URL | HMAC signature, minutes |
| `GET /m/{sha256}[/{variant}].{ext}` | a public file, immutable, year-long cache | none |
| `GET /s/{token}` · `POST /s/{token}/unlock` · `GET /s/{token}/zip` | share page, gate unlock, download all | share gates; planned |
| `GET /r/{token}` · `POST /r/{token}` | file request page and upload | request gates; planned |

- on the shared host's default hostname the tenant prefixes the path: `/t/{tenant}/m/…`, `/t/{tenant}/s/…`.
- every served response carries `X-Content-Type-Options: nosniff`, a sandbox CSP, and `attachment` for active types.

## Events

`IEvent` records, published through the be sdk outbox; planned with the events backlog item.

`FileUploaded` · `FileReady` · `FileRejected` · `FileDeleted` · `FileRestored` · `FilePurged` · `VariantReady` ·
`ArchiveReady` · `ShareCreated` · `ShareOpened` · `ShareDownloaded` · `ShareRevoked` · `QuotaWarning` ·
`QuotaExceeded` · `TenantMoved`.

## Errors

`FilesErrors` today: `FileNotFound` · `FileNotReady` · `FileTooLarge` · `TypeNotAllowed` · `ContentMismatch` ·
`NameInvalid` · `ReferenceNotFound` · `UrlInvalid` · `TenantSuspended` · `TenantInvalid` · `CursorInvalid` ·
`RangeInvalid`. Planned: `QuotaExceeded` · `ScanFailed` ·
`UploadExpired` · `ChecksumMismatch` · `ShareExpired` · `ShareLocked` · `ShareExhausted`.

## Forms

| Form | Implementation | Wire |
|---|---|---|
| 1 | `Module…Broker` (Module), delivery routes from Server | in process, over the consumer's database and core `IBlobRepository` |
| 2/3 | `Api…Broker` (Client) | the HTTP API above |
| fe | `fe-files` client and kits | the same API through the product's backend tickets |

## Conformance

- `Files.Tests.Conformance` runs one suite (`packages/Testing`) against the module in process and the released image.
- beyond the brokers: tenant isolation on the shared host, signed URL expiry, gate bypass attempts, range requests,
  magic-byte mismatches, archive bombs.
- until v0.1 iteration 6, the template's Greetings sample (`IGreetingsBroker`) still runs its own suite.
