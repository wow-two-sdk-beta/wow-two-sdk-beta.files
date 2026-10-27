# deployment/

How wow-two-sdk-beta.files's service ships. Descriptor format: wow-two-ws/conventions/deployment/descriptor/deploy-descriptor.md.
The packages ship separately, from `publish-packages.yml` (`engineering/scripts/scripts.md`).

| File | Role |
|---|---|
| `deploy.yml` | descriptor 2: one service, `files` (`/health`, 256 MB, the typed required key `DatabaseSettings:ConnectionString`, a `data` volume); sites `app` (public, primary) and `console` (private), both at `/` and `/api`; `dotnetEnvironment: true` and the `verify` dev check |
| `Dockerfile` | three stages: build the frontend workspace (`fe-files`, then `apps/web`), publish `service/Files.Api` with its packages and the pages in `wwwroot`, run on `aspnet` with `curl` |
| `docker-compose.yml` | local verification: the image with its SQLite file on a named volume, on `http://127.0.0.1:8393` |

## Contract with Wheelhouse

- Image: `ghcr.io/<owner>/<repo>/files`, published by `.github/workflows/publish-docker-image.yml` through the shared
  `delivery.yml` of `wow-two-platform.pipelines` (pinned `v0.10.0`); CI never deploys. `v0.10.0` carries the typed
  setting keys (`settings.keys`) into the bundle; the runner needs Wheelhouse v0.5 or later.
- Version: `X.Y` from the newest `planning/version-track/v{X.Y}/`; the image tag and `GET /api/system/status` report the
  same `X.Y.Z`, plus the build's commit after a `+`. Package releases (`be-v*`, `fe-v*`) never move it.
- Settings a target provides in `/app/appsettings.Target.json`: `DatabaseSettings:ConnectionString`
  (`Data Source=/data/files.db`); `Deployment:TrustedProxies` or `Deployment:TrustedNetworks` names the ingress.
- Bytes: `Storage:Root` (default `/data/blobs`) on the `data` volume, beside the SQLite file.
- File links: `Files:Delivery:SigningKey` (secret, 32+ characters; rotating it voids issued links); links point at
  `Sites:app:Url` unless `Files:Delivery:BaseUrl` names another content host.
- Callers: `Files:Auth:ApiKeys:{n}:{Id,Sha256,TenantId,AppId,Scopes:{m}}` (the key's SHA-256, never the key) and
  `Files:Auth:Issuers:{n}:{Issuer,Audience,Authority,TenantId}`; with neither, the API answers 401. Both move to the
  tenants table with the admin API.
- Migrations: the host applies them at boot on every run (`ApplyWhenDeployed`, event 3014) until the deploy runs a
  migrate step.
- Database: SQLite on the `data` volume, one instance per environment; the host applies the module's pending
  migrations before it serves. `GET /health` answers 200 only while the database answers.
- Dev check: after a dev rollout, `verify` curls `$VERIFY_SERVICE_FILES/health` and `/api/greetings` with
  `Host: localhost`; `HostFilteringTests` replays the descriptor's requests.
- After each main image publish, `conformance.yml` runs the contract suite against the released image.

## Local run

- `appsettings.Development.json` holds a local-only key: send `X-Api-Key: files-local-dev-key` to act in the
  `default` tenant with every scope. No deployment accepts it.

```bash
docker build --build-arg APP_VERSION=0.1.99 --build-arg VCS_REF=$(git rev-parse HEAD) \
  -f engineering/deployment/Dockerfile -t files:local engineering/codebase
```
