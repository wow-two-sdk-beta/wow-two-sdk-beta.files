# Imports

*Last updated: 2026-11-09*

> A product's legacy files move into a Files tenant: Wheelhouse opens an import, runs the mover on the source host, and
> the product's cutover reads the id map. Shapes agreed with the wheelhouse lane (`files-integration` #38; t9 #2–#7);
> P1 is the dev slice.

## Case

- Ocharo's Brand, Catalogue and Desk files move into tenant `ocharo` on B (`ocharo-platform`
  `engineering/deployment/files-migration.yml`).
- Wheelhouse runs the job; Files owns the API, the import token and the mover (t9 #2).
- A product without Wheelhouse runs the same mover by hand.

## Who calls

| Caller | Credential | Reaches |
|---|---|---|
| Wheelhouse | the operator key: `X-Api-Key: files_` + `Files:Operator:ApiKey` | open an import, mint its tokens, read it |
| the mover | the import token: `X-Import-Token` | push files into its import, read it |

- `Files:Operator:ApiKey` holds 32 hex characters (`generate: { random: 16, encoding: hex }`); Files registers it as the
  key `files_{value}` with the one scope `files.tenants` and no tenant, so it reaches no file endpoint.
- The import token is Files' own, never an Identity token: `fimp_{payload}.{mac}`, the payload base64url JSON
  `{ "imp": id, "ten": tenant, "exp": unix seconds }`, the mac HMAC-SHA256 under a key derived from
  `Files:Delivery:SigningKey` (HKDF, info `files-import-token`). It lives `Files:Imports:TokenLifetime` (default 6 h).

## API

Under `/api/files/v1`, the be sdk `{ data }` envelope and JSON preset.

| Route | Caller | Body | Answer |
|---|---|---|---|
| `POST /imports` | operator | `{ "tenant": "ocharo", "sources": ["brand-media"] }` | `201 { "id", "token", "expiresAt" }` |
| `POST /imports/{id}/tokens` | operator | — | `200 { "token", "expiresAt" }`; `404` unknown |
| `GET /imports/{id}` | operator, its token | — | `200 { "id", "tenant", "state", "sources": [{ "id", "files", "bytes", "failed" }] }` |
| `GET /imports/{id}/map?cursor=&limit=` | operator, its token | — | `200 { "items": [{ "source", "key", "reference", "fileId", "sha256", "size" }], "next" }` |
| `POST /imports/{id}/files` | its token | multipart, below | `201` stored · `200` already there · `409` · `422` |

- `tenant` matches `FilesConstants.TenantIdPattern`; `sources` lists 1–50 distinct ids; `state` is `open` in P1.
- A token for another import, an expired one or a bad mac answers `401`.

### `POST /imports/{id}/files`

Fields before the file part, as `POST /files` reads them:

| Field | Required | Meaning |
|---|---|---|
| `source` | yes | one of the import's sources |
| `key` | yes | the source key; a composite key joins its values with `/` |
| `sha256` | yes | lowercase hex of the bytes; Files checks it |
| `reference` | no | attached to the file, as `IFileReferencesBroker.AttachAsync` does |
| `name` | no | default: the key's last segment |
| `contentType` | no | default: detected from the bytes |
| `app` | no | default: the reference's text before its first `.` |

- Answer `{ "fileId", "sha256", "size", "created" }`.
- Idempotent per (import, source, key): the same SHA-256 again answers `200` with the stored file; another SHA-256
  answers `409`; bytes whose hash differs from `sha256` answer `422` and count as failed.
- The file uploads as `Private` through the module, so dedup, type checks, variants and audit run as for any upload; the
  audit subject is `import:{id}`.

### Storage

Migration `007-imports`:

- `files_imports`: `id`, `tenant_id`, `state`, `created_at_utc`.
- `files_import_sources`: `import_id`, `source_id`, `failed`.
- `files_import_items`: `import_id`, `source_id`, `source_key`, `file_id`, `sha256`, `size`, `reference`,
  `created_at_utc`; unique (`import_id`, `source_id`, `source_key`).

## Mover

`dotnet Files.Api.dll migrate plan|push|verify`, the Files service image itself, one source per run. It reads no
settings file and starts no web host.

Standard input, one JSON document:

```json
{
  "files": "https://files.{tenant domain}/",
  "import": "<import id>",
  "token": "<import token; push and verify only>",
  "source": { "id": "desk-preparation-media", "kind": "postgres-column", "...": "the manifest source as written" },
  "connection": "<PostgreSQL connection string, read-only; postgres-column only>",
  "root": "/source"
}
```

| Kind | Reads | Key | Placeholders |
|---|---|---|---|
| `volume-folder` | every file under `{root}/{path}` | the path below it, `/`-separated | `{path}`, `{sha256}` |
| `sqlite-column` | `{root}/{database}`, opened read-only: `table`, `key`, `content`, optional `name`, `type` | the key columns' values | each key column, `{sha256}` |
| `postgres-column` | `connection`: the schema-qualified `table`, `key`, `content`, optional `name`, `type` | the key columns' values | each key column, `{sha256}` |

- `key` is one column or a list; `reference` is a template over the placeholders.
- A missing `type` lets Files detect the type from the bytes.
- A `volume-folder` whose folder does not exist reads as an empty source (0 files, exit `0`, a note on stderr): a
  product creates its blob folders on its first write. A missing `root` or a `path` that names a file is refused.
- A `sqlite-column` database in WAL mode with no `-wal` file beside it (its product stopped) opens `immutable`, so a
  read-only mount needs no `-wal` or `-shm` file; a live one reads through its `-wal`. The mover never writes beside
  the source.

| Verb | Does |
|---|---|
| `plan` | counts files and bytes; touches nothing remote |
| `push` | reads the map once, skips keys already there, uploads the rest with their SHA-256 |
| `verify` | pages the map and compares each key's SHA-256 with the source; a missing or different key fails |

- stdout: JSON lines, `{"progress":{"files":n,"bytes":b}}` at most every 2 s, last
  `{"result":{"files":n,"bytes":b,"skipped":s,"failed":f}}`.
- stderr: counts and source keys only, never contents or a secret.
- exit `0` done; `1` refused input, never retried; `2` failed, a rerun resumes.
