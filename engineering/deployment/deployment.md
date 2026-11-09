# deployment/

How wow-two-sdk-beta.files's service ships. Descriptor format: wow-two-ws/conventions/deployment/descriptor/deploy-descriptor.md.
The packages ship separately, from `publish-packages.yml` (`engineering/scripts/scripts.md`).

| File | Role |
|---|---|
| `deploy.yml` | descriptor 2: one service, `files` (`/health`, 256 MB, `needs: [postgres]`, the typed required key `DatabaseSettings:ConnectionString`, a `data` volume for the bytes); sites `app` (public, primary) and `console` (private), both at `/` and `/api`; `dotnetEnvironment: true` and the `verify` dev check |
| `Dockerfile` | three stages: build the frontend workspace (`fe-files`, then `apps/web`), publish `service/Files.Api` with its packages and the pages in `wwwroot`, run on `aspnet` with `curl` |
| `docker-compose.yml` | local verification: the image on its own PostgreSQL (`postgres:18-alpine`), bytes on a named volume, on `http://127.0.0.1:8393` |

## Contract with Wheelhouse

- Image: `ghcr.io/<owner>/<repo>/files`, published by `.github/workflows/publish-docker-image.yml` through the shared
  `delivery.yml` of `wow-two-platform.pipelines` (pinned `v0.15.0`); CI never deploys. It carries the typed
  setting keys (`settings.keys`) into the bundle, and `generate` for the operator key (since `v0.14.0`).
- Version: `X.Y` from the newest `planning/version-track/v{X.Y}/`; the image tag and `GET /api/system/status` report the
  same `X.Y.Z`, plus the build's commit after a `+`. Package releases (`be-v*`, `fe-v*`) never move it.
- Settings a target provides in `/app/appsettings.Target.json`: `DatabaseSettings:ConnectionString`, which Wheelhouse
  fills from the target's PostgreSQL database (`needs: [postgres]`) and keeps in its vault;
  `Deployment:TrustedProxies` or `Deployment:TrustedNetworks` names the ingress. `DB_CONNECTION` wins over the key.
- Bytes: `Storage:Root` (default `/data/blobs`) on the `data` volume.
- File links: `Files:Delivery:SigningKey` (secret, 32+ characters; rotating it voids issued links); links point at
  `Sites:app:Url` unless `Files:Delivery:BaseUrl` names another content host.
- Callers: `Files:Auth:Issuers:{n}:{Issuer,TenantId,Audiences:{m},MetadataAddress,Jwks}` (each tenant's issuer and its
  own `https://files.{domain}/` audience, with the slash) and `Files:Auth:Audience` (the instance's own URI, for an issuer
  that names none); `Files:Auth:ApiKeys:{n}:{Id,Sha256,TenantId,AppId,OrganizationId,ExpiresAt,Scopes:{m}}` for callers
  outside Identity (the SHA-256 of a `files_` key, never the key). With neither, the API answers 401. Both move to the
  tenants table with the admin API.
- Signals: each tenant's Identity posts to `{audience}signals/identity`, the URL its registry lists as `SignalUrl`.
- Migrations: the module's set `files` (`Migrations/Postgres/`, history `files_migration_history`); the host creates
  the database when missing and applies the set at boot on every run (`ApplyWhenDeployed`, event 3014). The
  descriptor has no `migrate:` step, since the image's `migrate` verb is the file mover.
- Database: PostgreSQL, the target's own, one per environment (`DatabaseSettings:Provider` defaults to `Postgres`;
  SQLite is banned for a deployed service). `GET /health` answers 200 only while the database answers.
- Dev check: after a dev rollout, `verify` curls `$VERIFY_SERVICE_FILES/health` and `/api/system/status` with
  `Host: localhost`; `HostFilteringTests` replays the descriptor's requests.
- After each main image publish, `conformance.yml` runs the contract suite against the released image.

## Local run

- Deployed dev runs as .NET Development, so `appsettings.Development.json` holds defaults only: no loopback address, key
  or engine choice. `DeployedDevelopmentTests` boots the host as deployed dev and fails on any of them.
- A local run's values live in `appsettings.Local.json` beside `appsettings.json`: this machine's, git-ignored, out of
  every image, and read only by the `https` launch profile (`LOCAL_SETTINGS=true`). It names the store, the local link
  host, a signing key and a local API key:

```json
{
  "DatabaseSettings": { "Provider": "Sqlite", "ConnectionString": "Data Source=files.db" },
  "Files": {
    "Delivery": { "BaseUrl": "https://localhost:8392", "SigningKey": "<32+ characters of your own>" },
    "Auth": {
      "ApiKeys": [
        { "Id": "local", "Sha256": "<SHA-256 of your key>", "TenantId": "default", "Scopes": [ "files.read", "files.write", "files.admin" ] }
      ]
    }
  }
}
```

- The key is `files_` plus 32 letters or digits; its hash is `printf '%s' "$KEY" | shasum -a 256`. Send it as `X-Api-Key`
  to act in the `default` tenant with every scope.
- The store may instead be a local PostgreSQL's `DatabaseSettings:ConnectionString`. A `files.db` from before the set
  `files` holds its history in `migration_history`; delete it, and the next run rebuilds it.

```bash
docker build --build-arg APP_VERSION=0.1.99 --build-arg VCS_REF=$(git rev-parse HEAD) \
  -f engineering/deployment/Dockerfile -t files:local engineering/codebase
```
