# wow-two-sdk-beta.files — Contract

*Last updated: 2026-11-09*

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
| storage key | `{tenant}/{sha256[..2]}/{sha256}` | a variant's bytes too, by their own SHA-256 |
| `UploadId` | UUIDv7 | valid until the upload session expires |
| share token | 22+ URL-safe characters | stored hashed |

## Brokers

| Broker | Calls | State |
|---|---|---|
| `IFileUploadsBroker` | `UploadAsync(stream, spec)` | Module, Client |
| `IFileStoreBroker` | `GetAsync` · `OpenReadAsync(id, range?)` · `ListAsync(query)` · `UpdateAsync(patch)` · `DeleteAsync` · `RestoreAsync` | Module, Client |
| `IFileReferencesBroker` | `AttachAsync(id, ref)` · `DetachAsync` · `ListAsync(id)` | Module, Client |
| `IFileUrlsBroker` | `IssueAsync(id, spec)` → signed URL · `GetPublicUrlAsync(id, width?)` | Module, Client |
| `IFileUploadsBroker` tickets | `BeginAsync(spec)` → ticket · `CompleteAsync(uploadId)` · `CancelAsync` | planned |
| `IFileSharesBroker` | `CreateAsync(spec with gates)` · `UpdateAsync` · `RevokeAsync` · `ListAsync` · `AccessLogAsync` | planned |
| `IFileTransformsBroker` | `RequestAsync(spec)` → job, on-the-fly transforms | planned |
| `IFileArchivesBroker` | `ZipAsync(ids, spec)` → job · `StreamZipAsync(ids, output)` · `ExtractAsync(id, folder)` | planned |
| `IFileJobsBroker` | `GetAsync(jobId)` · `WaitAsync` | planned |
| `IFileAuditBroker` | `ListAsync(query)` → the tenant's audit trail, newest first | Module, Client |
| `IFileAdminBroker` | usage, quotas, policies, tenants | planned |

- models today: `FileModel`, `FileVariantModel`, `FileQuery`, `FilePage`, `FilePatch`, `FileContent`, `ByteRange`,
  `UploadSpec`, `FileReference`, `UrlSpec`, `SignedFileUrl`, `FileAuditEntryModel`, `FileAuditQuery`, `FileAuditPage`; enums `FileKind`,
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

### Variants

- an app policy names variant widths, `Files:Policies:{app}:Variants` (each 1–8192; empty makes none), and their WebP
  quality, `VariantQuality` (1–100, default 80); the instance refuses to start on an out-of-range value.
- an upload the registry recognizes as JPEG, PNG, WebP or GIF (first frame) gets one WebP per width no wider than the
  image, upright, without metadata, keeping the aspect ratio; a wider width is skipped, never upscaled. Core's
  `IImageService` (`AddImageProcessing`, limits in `Media:Images`) makes them; imports get them through the same upload.
- a failing variant (unreadable bytes, over the image limits, a store error) is logged and skipped; the upload succeeds.
- `FileModel.Variants` lists them narrowest first: `Width`, `Height`, `ContentType`, `Size`; one `files_variants` row
  each (migration 008), with its bytes a tenant blob under their own SHA-256.
- `UrlSpec.Width` and `GetPublicUrlAsync(id, width)` pick the narrowest variant at least that wide, else the widest;
  the original when no width is asked or the file has none; a width below 1 is a validation failure.
- a variant goes with its source: served while the source is live, ready and not deleted, public for a public URL;
  delete, restore and purge follow the source, and the sweep purges its bytes once no file or variant uses them.

## HTTP API

Every route sits under `/api/files/v1` (`FilesRouteConstants.Api`), written in full below; a caller joins it to the
instance URL (`https://files.{domain}/api/files/v1/files`). Every JSON `2xx` body is the be sdk envelope
`{ "data": … }`, so a caller reads `data`, never the top level; `…/content` answers the bytes. The service mirrors
the brokers.

| Method and path | Broker call | Scope |
|---|---|---|
| `POST /api/files/v1/files` (multipart) | `UploadAsync` | `files.write` |
| `GET /api/files/v1/files` · `GET /api/files/v1/files/{id}` | `ListAsync` · `GetAsync` | `files.read` |
| `GET /api/files/v1/files/{id}/content` | `OpenReadAsync` | `files.read` |
| `PATCH /api/files/v1/files/{id}` · `DELETE /api/files/v1/files/{id}` · `POST /api/files/v1/files/{id}/restore` | update · delete · restore | `files.write` |
| `PUT·DELETE /api/files/v1/files/{id}/references/{recordType}/{recordId}` · `GET /api/files/v1/files/{id}/references` | references | `files.write` · `files.read` |
| `POST /api/files/v1/files/{id}/urls` (`UrlSpec`) · `GET /api/files/v1/files/{id}/public-url?width=` | `IssueAsync` · `GetPublicUrlAsync` | `files.read` |
| `GET /api/files/v1/audit?fileId=&action=&cursor=&limit=` | audit `ListAsync` | `files.admin` |
| `POST /api/files/v1/uploads` · `POST /api/files/v1/uploads/{id}/complete` · `DELETE /api/files/v1/uploads/{id}` | tickets | planned |
| `POST /api/files/v1/shares` · `GET /api/files/v1/shares` · `PATCH·DELETE /api/files/v1/shares/{id}` | shares | `files.share`; planned |
| `GET /api/files/v1/usage` · `PUT /api/files/v1/policies/{app}` | admin | `files.admin`; planned |
| `GET·POST·PATCH /api/files/v1/tenants` · `POST /api/files/v1/tenants/{id}/keys` · `POST /api/files/v1/tenants/{id}/export` | shared host operator | `files.tenants`; planned |
| `POST /api/files/v1/imports` · `POST /api/files/v1/imports/{id}/tokens` · `GET /api/files/v1/imports/{id}` · `GET /api/files/v1/imports/{id}/map` · `POST /api/files/v1/imports/{id}/files` | imports, not a broker (`product/features/imports.md` § API) | operator key (`files.tenants`) · import token (`X-Import-Token`) |

- scopes: `FilesScopeConstants` (`files.read`, `files.write`, `files.share`, `files.admin`, `files.tenants`), what the
  app may ask; a person also needs the matching `FilesPermissionConstants` permission (`files:read`, `files:write`,
  `files:share`, `files:admin`); Identity issues both (T8 C3, § Policy shape).
- `Idempotency-Key` on every creating `POST`; cursor paging; filters `app`, `owner`, `kind`, `state`, `createdAfter`,
  `includeDeleted`.
- `POST /api/files/v1/files` reads its multipart body as a stream: fields (`name`, `appId`, `contentType`, `visibility`,
  `expiresAtUtc`) before the file part; the app policy's size limit stops the read.
- wire: the be sdk `{ "data": … }` envelope on every JSON `2xx` and its JSON preset; `FileId` as its string; `FileKind`
  in kebab case (`raw-photo`, `model-3d`); a problem carries `code` = the `AppErrorType`, which the Client maps back.
- callers: a token from a trusted issuer (`Files:Auth:Issuers`) — client credentials for a service, token exchange for a
  person — or, for a tenant outside Identity, an API key in `X-Api-Key`; the issuer's or the key's tenant is the
  caller's tenant (`partition`), never a claim the caller sends. Each action names its scope policy.
- a caller bound to an app (`client_id`, a key's app) uploads under that app only (`AppNotAllowed`).
- Identity's revocation signals arrive at `POST /signals/identity` under each tenant's host; a revoked person's token is
  refused until it would have expired.
- the Client implements the brokers by hand over one named `HttpClient` (`files`), sending a tenant API key or Identity
  tokens (`FilesClientOptions.Identity`): an exchanged token when the call carries a person, client credentials
  otherwise or inside `FilesClientCalls.AsService()`. OpenAPI comes from the host.

## Hosting routes

Served by `packages/Server` on the content host (`files.{domain}` or the shared host), never on an app's origin.

| Route | Serves | Auth |
|---|---|---|
| `GET /f/{fileId}?exp=&d=&w=&sig=` | a private file through a signed URL; with `w`, its variant of that width | HMAC-SHA256 over tenant, id, expiry, disposition and `w`; up to `MaxLifetime` |
| `GET /m/{sha256}.{ext}` | a public file, or a public file's variant by the variant's SHA-256; immutable, year-long cache | none; a public, ready file of the tenant must hold the bytes or have the variant |
| `GET /s/{token}` · `POST /s/{token}/unlock` · `GET /s/{token}/zip` | share page, gate unlock, download all | share gates; planned |
| `GET /r/{token}` · `POST /r/{token}` | file request page and upload | request gates; planned |

- a tenant other than the instance's default prefixes the path: `/t/{tenant}/f/…`, `/t/{tenant}/m/…`, `/t/{tenant}/s/…`.
- every served response carries `X-Content-Type-Options: nosniff`, `default-src 'none'; style-src 'unsafe-inline';
  sandbox`, `Cross-Origin-Resource-Policy: cross-origin` and `no-referrer`; active and executable types are always
  `attachment`; the routes override the be sdk preset's headers per endpoint.
- one `Range` is served as `206`, several as the whole file, one outside the file as `416`; the `ETag` is the SHA-256.
- `GET` and `HEAD`; the delivery seam is `IFilesDeliveryService` in the Module, the routes `MapFilesDelivery()` in Server.

The service also serves the icon set from its web root, for any origin to embed:

| Route | Serves | Auth |
|---|---|---|
| `GET /icons/{ext}.svg` | a registry extension's icon, labelled; static file built by `apps/web` | none; `Cross-Origin-Resource-Policy: cross-origin` |
| `GET /icons/kinds/{kind}.svg` | a kind's icon, unlabelled, for small sizes | same |

- any other lowercase `{name}.svg` under either path answers the generic kind's icon; anything else under `/icons/` is
  `404`. Extensions are lowercase, as `extensionOf` returns them.

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
- `FileVariantsConformance` runs where the instance gives the app `conformance-variants` the widths 480 and 900
  (`Files:Policies:conformance-variants:Variants`): variants per width, never upscaled, the width a URL picks, and a
  deleted source serving none.
