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
| `IFileUploadsBroker` | `UploadAsync(stream, spec)` | Module, Client |
| `IFileStoreBroker` | `GetAsync` · `OpenReadAsync(id, range?)` · `ListAsync(query)` · `UpdateAsync(patch)` · `DeleteAsync` · `RestoreAsync` | Module, Client |
| `IFileReferencesBroker` | `AttachAsync(id, ref)` · `DetachAsync` · `ListAsync(id)` | Module, Client |
| `IFileUrlsBroker` | `IssueAsync(id, spec)` → signed URL · `GetPublicUrlAsync(id, variant?)` | Module, Client |
| `IFileUploadsBroker` tickets | `BeginAsync(spec)` → ticket · `CompleteAsync(uploadId)` · `CancelAsync` | planned |
| `IFileSharesBroker` | `CreateAsync(spec with gates)` · `UpdateAsync` · `RevokeAsync` · `ListAsync` · `AccessLogAsync` | planned |
| `IFileTransformsBroker` | `GetVariantAsync(id, name)` · `RequestAsync(spec)` → job | planned |
| `IFileArchivesBroker` | `ZipAsync(ids, spec)` → job · `StreamZipAsync(ids, output)` · `ExtractAsync(id, folder)` | planned |
| `IFileJobsBroker` | `GetAsync(jobId)` · `WaitAsync` | planned |
| `IFileAuditBroker` | `ListAsync(query)` → the tenant's audit trail, newest first | Module, Client |
| `IFileAdminBroker` | usage, quotas, policies, tenants | planned |

- models today: `FileModel`, `FileQuery`, `FilePage`, `FilePatch`, `FileContent`, `ByteRange`, `UploadSpec`,
  `FileReference`, `UrlSpec`, `SignedFileUrl`, `FileAuditEntryModel`, `FileAuditQuery`, `FileAuditPage`; enums `FileKind`,
  `FileState`, `FileVisibility`, `FileDisposition`, `FileAuditAction`, `FileAuditOutcome`.
- every upload (refused ones too), read, change, delete and restore writes an audit entry; a URL redemption writes one
  with no subject.
- `FileState`: `Uploading` → `Pending` → `Ready`, or `Rejected` · `Quarantined`; a soft delete sets `DeletedAtUtc`.
- the stored content type is the registry's for the recognized type, never the uploader's; a rename may change the
  extension only to one of the same content type.
- a file lives while any record references it: expiry waits for the last reference. Per app, opt-in: an
  unreferenced file purges `OrphanRetention` after its upload or last detach; uploads expire `DefaultLifetime` after
  upload unless they set their own expiry.
- a file past its purge or orphan time, or expired with no reference, is gone for every call before the sweep purges it.
- share gates, once declared, are a closed set: expiry, password, max downloads, sign-in, email; later IP and terms.

## HTTP API

Base `/api/files/v1` (`FilesRouteConstants.Api`); the service mirrors the brokers.

| Method and path | Broker call | Scope |
|---|---|---|
| `POST /files` (multipart) | `UploadAsync` | `files.write` |
| `GET /files` · `GET /files/{id}` | `ListAsync` · `GetAsync` | `files.read` |
| `GET /files/{id}/content` | `OpenReadAsync` | `files.read` |
| `PATCH /files/{id}` · `DELETE /files/{id}` · `POST /files/{id}/restore` | update · delete · restore | `files.write` |
| `PUT·DELETE /files/{id}/references/{recordType}/{recordId}` · `GET /files/{id}/references` | references | `files.write` · `files.read` |
| `POST /files/{id}/urls` · `GET /files/{id}/public-url` | `IssueAsync` · `GetPublicUrlAsync` | `files.read` |
| `GET /audit?fileId=&action=&cursor=&limit=` | audit `ListAsync` | `files.admin` |
| `POST /uploads` · `POST /uploads/{id}/complete` · `DELETE /uploads/{id}` | tickets | planned |
| `POST /shares` · `GET /shares` · `PATCH·DELETE /shares/{id}` | shares | `files.share`; planned |
| `GET /usage` · `PUT /policies/{app}` | admin | `files.admin`; planned |
| `GET·POST·PATCH /tenants` · `POST /tenants/{id}/keys` · `POST /tenants/{id}/export` | shared host operator | `files.tenants`; planned |
| `POST /imports` · `GET /imports/{id}` | migration | `files.admin`; planned |

- scopes: `FilesScopeConstants` (`files.read`, `files.write`, `files.share`, `files.admin`, `files.delegate`,
  `files.tenants`); Identity issues them (T8 C3).
- `Idempotency-Key` on every creating `POST`; cursor paging; filters `app`, `owner`, `kind`, `state`, `createdAfter`,
  `includeDeleted`.
- `POST /files` reads its multipart body as a stream: fields (`name`, `appId`, `contentType`, `visibility`,
  `expiresAtUtc`) before the file part; the app policy's size limit stops the read.
- wire: the be sdk `{ data }` envelope and JSON preset; `FileId` as its string; `FileKind` in kebab case
  (`raw-photo`, `model-3d`); a problem carries `code` = the `AppErrorType`, which the Client maps back.
- callers: a tenant API key in `X-Api-Key`, or a client-credentials token from a trusted issuer (`Files:Auth`); the
  key's or the issuer's tenant is the caller's tenant, never a claim the caller sends. Each action names its scope.
- a caller bound to an app (`azp`, a key's app) uploads under that app only (`AppNotAllowed`).
- the Client implements the brokers by hand over one named `HttpClient` (`files`); its builder takes
  `AddOAuth2ClientCredentials`. OpenAPI comes from the host.

## Hosting routes

Served by `packages/Server` on the content host (`files.{domain}` or the shared host), never on an app's origin.

| Route | Serves | Auth |
|---|---|---|
| `GET /f/{fileId}?exp=&d=&sig=` | a private file through a signed URL; variants later | HMAC-SHA256 over tenant, id, expiry, disposition; up to `MaxLifetime` |
| `GET /m/{sha256}.{ext}` | a public file, immutable, year-long cache; variants later | none; a public, ready file of the tenant must hold the bytes |
| `GET /s/{token}` · `POST /s/{token}/unlock` · `GET /s/{token}/zip` | share page, gate unlock, download all | share gates; planned |
| `GET /r/{token}` · `POST /r/{token}` | file request page and upload | request gates; planned |

- a tenant other than the instance's default prefixes the path: `/t/{tenant}/f/…`, `/t/{tenant}/m/…`, `/t/{tenant}/s/…`.
- every served response carries `X-Content-Type-Options: nosniff`, `default-src 'none'; style-src 'unsafe-inline';
  sandbox`, `Cross-Origin-Resource-Policy: cross-origin` and `no-referrer`; active and executable types are always
  `attachment`; the routes override the be sdk preset's headers per endpoint.
- one `Range` is served as `206`, several as the whole file, one outside the file as `416`; the `ETag` is the SHA-256.
- `GET` and `HEAD`; the delivery seam is `IFilesDeliveryService` in the Module, the routes `MapFilesDelivery()` in Server.

## Events

`IEvent` records, published through the be sdk outbox; planned with the events backlog item.

`FileUploaded` · `FileReady` · `FileRejected` · `FileDeleted` · `FileRestored` · `FilePurged` · `VariantReady` ·
`ArchiveReady` · `ShareCreated` · `ShareOpened` · `ShareDownloaded` · `ShareRevoked` · `QuotaWarning` ·
`QuotaExceeded` · `TenantMoved`.

## Errors

`FilesErrors` today: `FileNotFound` · `FileNotReady` · `FileTooLarge` · `TypeNotAllowed` · `ContentMismatch` ·
`NameInvalid` · `ReferenceNotFound` · `UrlInvalid` · `TenantSuspended` · `TenantInvalid` · `CursorInvalid` ·
`RangeInvalid` · `ReferenceInvalid` · `FileNotPublic` · `VariantNotFound` · `AppNotAllowed`. Planned: `QuotaExceeded` · `ScanFailed` ·
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
- `FilesBrokerConformance` runs against the Module and against the Client over the host by two tenants' API keys;
  the workflow runs the Client side against the released image with two test-only keys.
- until v0.1 iteration 6, the template's Greetings sample (`IGreetingsBroker`) still runs its own suite.
