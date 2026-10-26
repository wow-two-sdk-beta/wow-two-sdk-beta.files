# T4 — Contract

*Last updated: 2026-10-26*

> The one client contract every form shares: the brokers in `Files.Abstractions`, the HTTP API that mirrors them,
> the events, the URL shapes and the errors. A product moves between Form 1, an A and a B by configuration only,
> per capability forms (`wow-two-ws/conventions/development/capability-forms.md`) § Contract.

- Package names follow [T6](t6-library-shape.md); `(U)` = unverified.

---

## 1 · Identifiers

- `FileId`: UUIDv7, assigned by the instance, never naming the instance; survives every tenant move.
- `TenantId`, `OrganizationId`, `AppId`: strings from the tenant registry and token claims.
- storage key: `{tenant}/{sha256[..2]}/{sha256}`; variants `{tenant}/v/{sha256}/{variant}.{ext}`.
- `ShareToken`: 22+ random URL-safe characters, stored hashed.
- `UploadId`: UUIDv7, valid until the session's expiry.

---

## 2 · Brokers

| Broker | Calls | Phase |
|---|---|---|
| `IFileUploads` | `UploadAsync(stream, spec)` · `BeginAsync(spec)` → ticket (URL, method, headers, expiry, max size) · `CompleteAsync(uploadId)` · `CancelAsync` | P1, direct and tus tickets P2 |
| `IFileStore` | `GetAsync` · `OpenReadAsync(id, range?)` · `ListAsync(query)` · `UpdateAsync(patch)` · `DeleteAsync` · `RestoreAsync` | P1 |
| `IFileReferences` | `AttachAsync(id, ref)` · `DetachAsync` · `ListAsync(id)` | P1 |
| `IFileUrls` | `IssueAsync(id, variant?, lifetime, disposition)` → signed URL · `PublicUrl(id, variant?)` | P1 |
| `IFileShares` | `CreateAsync(spec with gates)` · `UpdateAsync` · `RevokeAsync` · `ListAsync` · `AccessLogAsync` | P1, log P2 |
| `IFileTransforms` | `GetVariantAsync(id, name)` · `RequestAsync(spec)` → job | P1 presets, P2 ad hoc |
| `IFileArchives` | `ZipAsync(ids, spec)` → job → file · `StreamZipAsync(ids, output)` · `ExtractAsync(id, folder)` | P1 zip, P2 extract |
| `IFileJobs` | `GetAsync(jobId)` · `WaitAsync` | P1 |
| `IFileAdmin` | usage, quotas, policies, audit, tenants (B) | P1 usage and audit, P2 rest |

- every call carries the acting principal from the kernel `ICurrentUserService`; the Client forwards the caller's
  token or its client-credentials token.
- results use the kernel `Result` / `AppError`; HTTP status mapping stays product-side (`ApiResults.ToStatusCode`).
- models: `FileInfo`, `UploadSpec`, `UploadTicket`, `FilePatch`, `FileQuery`, `Reference`, `ShareSpec`, `ShareGate`,
  `VariantSpec`, `TransformSpec`, `ArchiveSpec`, `JobInfo`, `Usage`.
- `ShareGate` is a closed set: `Expiry`, `Password`, `MaxDownloads`, `SignIn(org)`, `Email(list)`, later `Ip`, `Terms`.

---

## 3 · HTTP API

Base `/api/files/v1`; the service mirrors the brokers, plus hosting routes.

| Method and path | Broker call | Scope |
|---|---|---|
| `POST /files` (multipart) | `UploadAsync` | `files.write` |
| `POST /uploads` | `BeginAsync` | `files.write` |
| `POST /uploads/{id}/complete` · `DELETE /uploads/{id}` | `CompleteAsync` · `CancelAsync` | upload ticket |
| `POST·PATCH·HEAD /uploads/tus/{id}` | tus 1.0 | upload ticket (P2) |
| `GET /files` · `GET /files/{id}` | `ListAsync` · `GetAsync` | `files.read` |
| `GET /files/{id}/content` | `OpenReadAsync` | `files.read` |
| `PATCH /files/{id}` · `DELETE /files/{id}` · `POST /files/{id}/restore` | update · delete · restore | `files.write` |
| `PUT·DELETE /files/{id}/references/{ref}` · `GET /files/{id}/references` | references | `files.write` · `files.read` |
| `POST /files/{id}/urls` | `IssueAsync` | `files.read` |
| `GET /files/{id}/variants/{name}` | `GetVariantAsync` | `files.read` |
| `POST /transforms` · `POST /archives` · `GET /jobs/{id}` | jobs | `files.write` · `files.read` |
| `POST /shares` · `GET /shares` · `PATCH·DELETE /shares/{id}` | shares | `files.share` |
| `GET /usage` · `GET /audit` · `PUT /policies/{app}` | admin | `files.admin` |
| `GET·POST·PATCH /tenants` · `POST /tenants/{id}/keys` · `POST /tenants/{id}/export` | B operator | `files.tenants` |
| `POST /imports` · `GET /imports/{id}` | migration | `files.admin` |

- OpenAPI comes from the host; the Client is not generated — it implements the brokers by hand over the be sdk HTTP
  client, as the identity Client does (U).
- `Idempotency-Key` header on every `POST` that creates something.
- pagination by cursor; filters `app`, `owner`, `type`, `state`, `createdAfter`, `tag`.

---

## 4 · Hosting routes (delivery role)

| Route | Serves | Auth |
|---|---|---|
| `GET /f/{fileId}[/{variant}]?exp=&sig=` | a private file through a signed URL | HMAC signature, minutes |
| `GET /m/{sha256}[/{variant}].{ext}` | a public file, immutable, year-long cache | none; on the tenant's host |
| `GET /m/{sha256}/x/{signed-params}.{ext}` | an on-the-fly transform (P2) | signature over the params |
| `GET /s/{token}` · `POST /s/{token}/unlock` · `GET /s/{token}/zip` | share page, gate unlock, download all | share gates |
| `GET /r/{token}` · `POST /r/{token}` | file request page and upload (P2) | request gates |

- hosting routes live on the content host (`files.{domain}` or B's host), never on an app's origin.
- on B's default host the tenant prefixes the path: `/t/{tenant}/m/…`, `/t/{tenant}/s/…`.

---

## 5 · Events

`IEvent` records in `Files.Abstractions`, published through the be sdk outbox; the same names go out as webhooks in
P2.

| Event | When |
|---|---|
| `FileUploaded` | bytes stored, checks pending |
| `FileReady` | checks passed, presets built |
| `FileRejected` | validation or scan failed |
| `FileDeleted` · `FileRestored` · `FilePurged` | lifecycle |
| `VariantReady` | an on-demand variant finished |
| `ArchiveReady` | a ZIP job finished |
| `ShareCreated` · `ShareOpened` · `ShareDownloaded` · `ShareRevoked` | sharing |
| `QuotaWarning` · `QuotaExceeded` | limits (P2) |
| `TenantMoved` | a tenant move finished (P2) |

---

## 6 · Errors

`FileNotFound` · `FileNotReady` · `FileTooLarge` · `TypeNotAllowed` · `ContentMismatch` · `QuotaExceeded` ·
`ScanFailed` · `UploadExpired` · `ChecksumMismatch` · `ShareExpired` · `ShareLocked` (gate required) ·
`ShareExhausted` · `TenantSuspended`.

- each is an `AppError` code in the shared validation vocabulary; texts come from the SDK message catalogue.

---

## 7 · Conformance

- `Files.Tests.Conformance` runs one suite against `Files.Module` in-process and against the released image.
- cases beyond the brokers: tenant isolation on B (a token from tenant X never reads tenant Y), signed URL expiry,
  gate bypass attempts, range requests, magic-byte mismatches, archive bombs.
- the suite ships in `Files.Testing` so a third party's instance can prove itself.
