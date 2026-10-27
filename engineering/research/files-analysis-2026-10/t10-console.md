# T10 — Console

*Last updated: 2026-10-27*

> The Files console is the web UI in `apps/web`, served by `Files.Api` at `/`. This track designs it before any page
> exists: who uses it in `Single` and `Shared` mode, which pages and stats, what the store must record to feed them,
> the shell, why sign-in comes last, and the smallest slice worth building after v0.1.

- Evidence: [spec](../../../product/product.md), [backlog](../../planning/backlog.md) § Console and neighbours,
  [T1](t1-services-tenancy.md), [T2](t2-feature-vector.md) § 15, [T5](t5-identity.md), [T8](t8-identity-contracts.md)
  C10, [contract](../../architecture/contract.md), [v0.1](../../planning/version-track/v0.1/v0.1.md); the Wheelhouse
  console and the fe sdk 0.0.33, read 2026-10-27 (paths relative to each repo's `engineering/codebase/`); `(U)` =
  unverified.
- Phase: `v0.1 data` = computable from what v0.1 stores (files, references, audit rows); `P1` · `P2` as in T2.
- Data source: `declared` = a row in contract.md's HTTP table (`planned` when the table says so); `new` = the console
  needs it and contract.md has no row; `exists` = in the code today.
- Console endpoints sit under `/api/files/v1`, same origin, scope `files.admin` unless a row says otherwise.
- Today's `ConsolePage.vue` is the template's Greeting sample; v0.1 iteration 6 deletes it.

---

## 1 · Audiences and modes

| Audience | Where | Scopes | Sees | Lands on |
|---|---|---|---|---|
| owner admin | an A (`Single`) | `files.admin` · `files.write` · `files.share` | its one tenant: every tenant page, plus System | Overview |
| shared-host operator | B (`Shared`) | `files.tenants` · `files.admin` | scope **All tenants** (aggregates), then any one tenant's pages; Tenants, Reports, System | Overview, All tenants |
| tenant admin | B, P2 (own issuer, C10) | `files.admin` of one tenant | an owner admin's pages for that tenant; no Tenants, Reports, System | Overview |
| app developer | A or B | a tenant admin, filtered to one app | Apps → app detail: policy, usage, rejections, connect snippet | Apps |
| share viewer · uploader | anyone | share or request gates | hosted `/s/{token}` and `/r/{token}` pages, outside the console shell | — |

- one SPA, one route table. The shell reads `GET /console/context` once: `mode`, tenants (B), the principal's scopes,
  the instance version and which pages are built.
- a nav item shows when mode, scope and build allow; it is hidden, never disabled.
- `Single`: the tenant is the one row; no switcher; the scope bar names the instance; Tenants and Reports do not exist.
- `Shared`: the scope bar carries the tenant switcher. The operator starts in All tenants, a tenant admin is fixed to
  their tenant.
- the SPA sends the chosen tenant in `X-Files-Tenant`; the API honours it only from a `files.tenants` principal. A
  tenant admin's tenant comes from the session.
- All-tenants pages: Overview, Usage, Audit, System, Imports, Tenants, Reports. Files, Shares, Apps and Settings need a
  tenant; in All tenants they show "Pick a tenant".
- moving a tenant from B to its own A changes nothing in the console: the mode comes from the server.
- app developer is not an audience of its own yet: Identity gives no per-app role (T8 C1 `Roles` is per organization).

---

## 2 · Page inventory

Stat ids `S1…S30` are § 3. 18 pages: 13 list pages, 5 detail pages.

| # | Page | Route | Audience | Purpose | Widgets and stats | Actions | Data source | Phase |
|---|---|---|---|---|---|---|---|---|
| 1 | Overview | `/` | all | what is stored, what changed, what needs a look | tiles S1 S2 S13 S16 S6 · Attention S12 S10 S11 S14 S28 S22 · uploads/day S13 · by kind S3 · top apps S5 · recent S9 · instance strip · operator: per-tenant table S24 | refresh; every count opens its filtered view | `GET /usage` declared, planned · `GET /usage/daily` new · `GET /files` declared · `GET /api/system/status` exists, extend | v0.1 data; All-tenants P1 on B |
| 2 | Files | `/files` | owner, tenant admin, operator in a tenant | find, preview and manage files | filters: name, app, owner, kind, state, visibility, date, size · views All · Trash · Expiring · Orphans · Rejected · Quarantined · list or grid · footer count and bytes · S4 | upload, download, copy signed URL, rename, delete, restore, ZIP the selection, share the selection | `GET /files` · `PATCH` · `DELETE` · `restore` declared; new params in § 2.1; `POST /archives` T4 only | v0.1 data; ZIP and Quarantined later |
| 3 | File detail | `/files/:id` | same | one file: its bytes, users and readers | preview (image, PDF) · facts · references · variants S19 · dedup siblings S30 · audit tail · shares of it | same as Files; purge now (typed confirm) | `GET /files/{id}` · `/references` · `/content` · `/urls` declared; `GET /audit?file=` declared, planned | v0.1 data; variants P1; video and audio preview P2 |
| 4 | Shares | `/shares` | owner, tenant admin | every share link and its state | S20: states, opens 7 d, unlock failures, expiring · table: files, gates, opens, downloads used of max, last opened, creator · token shown as prefix only (stored hashed) | revoke, extend expiry, edit gates | `GET /shares` · `PATCH·DELETE /shares/{id}` declared, planned | P1 with sharing |
| 5 | Share detail | `/shares/:id` | same | one link: gates, files, access | gate summary · files · opens and downloads per day · access log S21 | revoke, edit | `AccessLogAsync` broker declared; HTTP `new` | P1; log P2 |
| 6 | Usage | `/usage` | owner, tenant admin, operator | where bytes and traffic go, over time | storage and files over time S18 · uploads/day S13 · downloads and origin egress S16 S17 · dedup S6 S7 · by kind, app, org S3 S5 · largest S8 · trash S10 · quota meters S23 | range, group by; CSV export (P2) | `GET /usage` · `/usage/daily` · `/usage/duplicates` new | now-views v0.1 data; series need § 3.3 tables; quotas P2 |
| 7 | Audit | `/audit` | owner, tenant admin, operator | who did what, and what was refused | S28 action counts · table: time, action, outcome, actor, app, file, bytes · filters: action, outcome, actor, app, file, date | open file; all rows of an actor; CSV export (P2) | `GET /audit` declared, planned | v0.1 data |
| 8 | Apps | `/apps` | owner, tenant admin, app developer | the apps that use this tenant | per app: files, bytes, uploads 7 d, rejection rate 7 d S14, policy summary, key count | open app | `GET /usage?group=app` · `GET /policies` new | P1 read-only (policy by config) |
| 9 | App detail | `/apps/:app` | same | policy, usage, rejections, how to connect | policy table: max size, allowed kinds and MIME, variants, retention, requires-reference · usage series · rejections by reason · recent uploads · connect snippet (base URL, audience, scopes, `AddFilesClient`) | edit policy (P2) | `PUT /policies/{app}` declared, planned; `GET` new | P1 read; P2 edit |
| 10 | Imports | `/imports` | operator, owner admin | migration progress | S25: source kind, tenant, state, files and bytes done of total, failed, started, ETA | open | `GET /imports/{id}` declared; list new | P1 read-only (Ocharo cutover) |
| 11 | Import detail | `/imports/:id` | same | failures and the id map | progress bars · failures (checksum mismatch, type rejected) · token expiry · id-map download | cancel; retry failed (P2) | `GET /imports/{id}` declared; cancel, id map new | P1 read; actions P2 |
| 12 | System | `/system` | owner admin, operator | is the instance healthy | roles up · database and migration version · byte store kind, reachable, free space · jobs and sweeps S22 · backups S27 · limits in effect · versions | retry failed job; run sweep now (P2) | `/api/system/status` exists, extend · `GET /jobs` new | v0.1 data (version, mode, store); jobs P1 |
| 13 | File types | `/types` | all | what the instance recognizes; which app allows it | registry grid: icon, extension, MIME, kind, magic, preview, risk · filter by kind · "allowed by app" switch | none | `file-types.json` through `fe-files/icons`; allowed-by-app from `GET /policies` | v0.1 (iteration 7's icon page) |
| 14 | Settings | `/settings` | owner admin, tenant admin | how products connect | keys: prefix, scopes, app, last used, expiry · hostnames and TLS status · trusted issuers (B) · webhooks (P2) · console sign-in, read-only | create, rotate, revoke key; add hostname, issuer | tenant routes declared, planned (`files.tenants`); A: config read-only | keys and hostnames P1 on B |
| 15 | Tenants | `/tenants` | operator | every tenant on B | S24: state, hostnames, issuer, stored, files, uploads 7 d, egress 7 d, quota %, last activity | create, suspend, resume, enter | `GET·POST·PATCH /tenants` declared, planned | P1 on B |
| 16 | Tenant detail | `/tenants/:id` | operator | one tenant's setup | tiles · hostnames · issuers · keys (two live while rotating) · quota · byte-store target · export bundles | keys, hostname, issuer, quota, suspend, export, enter | `/tenants/{id}` · `/keys` · `/export` declared, planned | P1 on B; quota and export P2 |
| 17 | Reports | `/reports` | operator | abuse reports and takedowns | queue: reporter, target, reason, age | quarantine file, suspend share, suspend tenant | `new` | P2 |
| 18 | Sign-in | `/signin` | anyone | start a console session | admin-key field (stopgap) or "Continue with Identity" (C10) · denied and expired states | sign in, sign out | `new`, § 5 | stopgap with the first slice; C10 last |

- Trash, Expiring, Orphans, Rejected, Quarantined are saved filters of Files, not pages; the Overview counts link to them.
- outside the console: the share page and the file request page (hosted routes), starting an import (Wheelhouse, W1),
  HTTP error rate and latency (OpenTelemetry, later Monitoring), billing (D17, P3).
- later candidates, not rows yet: duplicates finder (P2, a Files view), folders and tags (P2, Files), webhook
  deliveries (P2, Settings), erasure requests (P2), usage events for billing (P3, Usage).

### 2.1 · Endpoint delta

| Endpoint | State | For |
|---|---|---|
| `GET /console/context` | new | mode, tenants, principal, scopes, built pages, sign-in options |
| `POST·DELETE /console/session` | new | stopgap sign-in and sign-out (§ 5) |
| `GET /usage` | declared, planned | now-view: totals, by `kind` · `state` · `app` · `org` · `tenant`, attention counts, window sums |
| `GET /usage/daily` | new | daily series from `storage_daily` and `usage_daily` |
| `GET /usage/duplicates` | new | top duplicated blobs |
| `GET /files` | declared | add `q`, `sort` (created · size · name), `view`, `visibility`, `minSize`, `maxSize`, `expiringBefore`, `orphaned` to `FileQuery` |
| `POST /files/{id}/purge` | new | purge now |
| `POST /archives` | T4 only | stream a ZIP of the selection |
| `GET /policies` | new | list per app (the contract has only `PUT`) |
| `GET /jobs` | new | list (T4 has `GET /jobs/{id}`) |
| `GET /imports` · `POST /imports/{id}/cancel` | new | list and cancel |
| `GET /shares/{id}/access` | new | access log (P2) |
| `GET /api/system/status` | exists | add `mode`, `roles`, store kind, migration version |

---

## 3 · Stats

### 3.1 · How a stat is computed

| Method | Use when | Examples |
|---|---|---|
| live | an indexed query over one tenant's rows answers in well under a second at the largest tenant's size (U) | stored bytes, counts, largest, dedup, orphans |
| snapshot | the state cannot be rebuilt later: one `storage_daily` row per tenant, app and kind, written nightly by the worker | storage and files over time |
| rollup | events summed by day into `usage_daily`, from audit rows and delivery counters | uploads, downloads, egress, rejects, share opens |
| counter | enforcement needs the number inside the upload transaction | quota use (P2) |

- All-tenants views read snapshots and rollups only; they never scan tenants' `files`.
- a one-tenant now-view stays live until a tenant outgrows it; the endpoint hides the swap.
- audit rows feed the rollups; rollups outlive the audit rows' retention (open, D14).
- the snapshot job starts with the first console build: earlier history rebuilds only for files still stored.
- days are UTC.

### 3.2 · Metrics

| Id | Metric | Pages | Method | Needs recorded | Phase |
|---|---|---|---|---|---|
| S1 | stored bytes: Σ size over distinct (tenant, SHA-256) of non-purged rows, trash included | Overview, Usage, Tenants | live → snapshot | `files.size` · `sha256` · `deleted_at`; purged rows leave the table | v0.1 data |
| S2 | files: ready, not deleted | Overview, Usage | live | `state` · `deleted_at` | v0.1 data |
| S3 | files and logical bytes by kind | Overview, Usage | live | `kind` | v0.1 data |
| S4 | files by state: uploading · pending · ready · rejected · quarantined | Files | live | `state` | v0.1 data; quarantined P2 |
| S5 | files and bytes by app, by organization | Overview, Usage, Apps | live | `app_id` · `organization_id` | v0.1 data |
| S6 | dedup: logical bytes (Σ rows), saved = logical − S1, ratio | Overview, Usage | live | same as S1 | v0.1 data |
| S7 | top duplicated blobs: SHA, copies, size, saved | Usage | live | same as S1 | v0.1 data |
| S8 | largest 20 files | Usage | live, index (tenant, size desc) | `size` | v0.1 data |
| S9 | recent uploads | Overview | live | `created_at` | v0.1 data |
| S10 | trash: files, **reclaimable** bytes (blobs whose every row is trashed), next purge date | Overview, Usage | live | `deleted_at` · retention policy | v0.1 data |
| S11 | expiring within 7 days | Overview, Files | live | `expires_at` | v0.1 data |
| S12 | orphans pending sweep: count, bytes, oldest age | Overview, Files | live | **`orphaned_at`** and the app policy's `requires_reference`; the references table | v0.1 data, once iteration 3 adds both |
| S13 | uploads per day: count, bytes, by app | Overview, Usage | rollup | audit `upload` rows with `bytes`, `app_id`, `outcome` | v0.1 data, if the audit shape (§ 3.3) lands |
| S14 | rejected uploads per day by reason | Overview, Usage, Apps | rollup | audit `outcome=rejected` and `error_code`: `TypeNotAllowed` · `ContentMismatch` · `FileTooLarge` · `NameInvalid` | v0.1 data |
| S15 | deletes, restores, purges per day | Usage, Audit | rollup | audit actions | v0.1 data |
| S16 | private downloads per day: count, bytes served | Overview, Usage, File detail | rollup | audit `download.private` with `bytes`, `status`, one row per signature | v0.1 data |
| S17 | public hits and origin egress per day, per tenant | Usage | rollup | in-memory counters at the delivery role, flushed to `usage_daily`; no row per hit | P1; per file P2 |
| S18 | stored bytes and files over time, by kind and app | Usage, Tenants | snapshot | `storage_daily` | new table |
| S19 | variant bytes against originals | Usage, File detail | live | `file_variants.size` | P1 with variants |
| S20 | shares: active · expired · revoked · exhausted, opens, downloads, unlock failures, expiring 7 d | Shares, Overview | live + rollup | `shares` (state, gates, `downloads_used`, `last_opened_at`) · audit `share.*` | P1 |
| S21 | share access log | Share detail | live | `share_access` (time, outcome, IP prefix, agent; country P3) | P2 |
| S22 | jobs: queued, running, failed 24 h, oldest queued age; last run of each sweep | System, Overview | live | job store (Hangfire on PostgreSQL (U)) · `sweep_runs` (name, start, end, files, bytes freed) | P1 with the worker |
| S23 | quota use: bytes, files, monthly egress per tenant, organization, app; soft and hard state | Usage, Tenants | counter | `tenant_usage` · policy limits · events `QuotaWarning` at 80 %, `QuotaExceeded` at 100 % | P2 |
| S24 | per-tenant row: S1, S2, uploads 7 d, egress 7 d, quota %, last activity | Overview, Tenants | snapshot + rollup | `storage_daily` · `usage_daily` · `tenants.last_activity_at` | P1 on B |
| S25 | import progress: files and bytes done of total, failed, ETA | Imports | counter on the import row | `imports` counts, updated per batch | P1 |
| S26 | scans: failed, quarantined | Files, Overview | live | `state` · `scan_result` | P2 |
| S27 | backups: last nightly copy, bytes, status | System | live | `backup_runs` | P1 ops |
| S28 | audit action counts 24 h by outcome (`ok` · `denied` · `rejected` · `failed`) | Audit, Overview | live | audit | v0.1 data |
| S29 | top files by downloads; top shares by opens | Usage, Shares | rollup | per-file counters | P2 |
| S30 | one file: reference count, dedup siblings, last download, downloads | File detail | live; downloads need a counter | `file_references` · audit · per-file counter (P2) | v0.1 data; downloads P2 |

### 3.3 · What the store records beyond `FileModel`

Audit row, written by v0.1 iteration 5. Its shape decides what the console can ever show:

| Column | Notes |
|---|---|
| `id` · `at` · `tenant_id` | UUIDv7, UTC |
| `action` | `upload` · `delete` · `restore` · `purge` · `update` · `download.private` · `url.issued`; later `share.*` · `key.*` · `tenant.*` · `policy.changed` · `import.*` · `console.signin` · `operator.access` |
| `outcome` · `error_code` | `ok` · `denied` · `rejected` · `failed`; the `FilesErrors` code |
| `actor_kind` · `actor_iss` · `actor_sub` · `client_id` | `service` · `person` · `apikey` · `console` · `system`; (`iss`, `sub`) per C15; `azp` |
| `app_id` · `file_id` | either may be null |
| `bytes` · `status` · `hits` | bytes actually moved; HTTP status; requests folded into the row |
| `request_id` · `detail` · `ip_prefix` | small JSON; a truncated address, personal data for P2 erasure |

- indexes: (`tenant_id`, `at` desc), (`tenant_id`, `action`, `at` desc), (`tenant_id`, `file_id`, `at` desc).
- one row per signed-URL redemption: range requests of one signature add to `bytes` and `hits`, so a video seek does not
  write 300 rows.
- public `/m/` hits write no audit row, only the delivery counters (S17).
- a denied request (bad signature, expired URL, suspended tenant, missing scope) writes `outcome=denied`, coalesced per
  address and minute so a probe cannot flood the table.
- console mutations write rows too, with `actor_kind=console`.

Other additions:

| Table or column | For |
|---|---|
| `files.orphaned_at` | set when the last reference detaches, or at `ready` when none exists; the sweep deletes after the grace window |
| app policy `requires_reference`, `retention_days`, `max_size`, `allowed_kinds` | S12, S10, the Apps page |
| `storage_daily` | tenant, day, app, kind, files, logical bytes, physical bytes, trash files, trash bytes |
| `usage_daily` | tenant, day, app, uploads, upload bytes, deletes, private downloads and bytes, public hits and bytes, rejects, denied, share opens, share downloads |
| `sweep_runs` · `backup_runs` · `imports` | S22 · S27 · S25 |
| `tenants.last_activity_at` | S24 |
| indexes on `files` | (`tenant_id`, `created_at` desc), (`tenant_id`, `size` desc), (`tenant_id`, `sha256`); partial on `deleted_at`, `expires_at`, `orphaned_at` |

### 3.4 · Traps

- public egress is origin egress only; a CDN (P2) hides hits unless its logs or provider stats are ingested.
- without the delivery counters the Downloads tile counts private files only and under-reports a public-image tenant.
- `/m/{sha}` hits carry no app: public traffic is per tenant, not per app.
- uploads per day from `files.created_at` under-counts once rows are deleted or purged; the audit rows are the source.
- dedup is per tenant: the operator's physical total is Σ of the tenants' S1.
- trash bytes are not Σ size of trashed rows: a blob another row still holds frees nothing.
- an unreferenced new upload is not an orphan until the grace window ends.
- the audit table takes one write per private download; keep rows narrow; retention follows D14.
- HTTP error rate and latency stay in OpenTelemetry; the console shows domain failures only.

---

## 4 · Navigation and shell

### 4.1 · Borrowed from Wheelhouse

| Piece | Wheelhouse | Files console |
|---|---|---|
| frame | `AppShell`: glass header with the brand block above the sidebar, collapse button, scope bar, profile menu; sidebar collapses to an icon rail (remembered); tablet drawer; phone bottom bar (`bootstrap/AppLayout.vue`) | the same frame |
| theme | `@import '@wow-two-beta/fe/themes.css'`, `theme-platform` on `<html>`, `dark` from the colour-mode provider (`bootstrap/index.css`) | `apps/web` imports `styles.css` only today; add the two lines |
| page recipe | `PageHeaderActions` (`RefreshButton`) → `DataRegion` (skeleton, error with retry, empty) → `Panel` blocks | the same; phones turn tables into cards (`Table` phone layout) |
| scope | `ScopeBar` breadcrumbs with the page's actions | tenant switcher as the first crumb on B |
| status | `StatusPill` over one status vocabulary | file state, share state, job state, tenant state |
| landing | Home leads with an attention summary | Overview leads with Attention |
| global places | profile menu: Activity, Secrets, Settings | profile menu: System, File types, Sign out |
| audit | `ActivityPage`, hash-chained, with a chain verdict | `/audit`; tamper evidence not planned (D14) |

### 4.2 · Reuse and gaps

| Need | In fe sdk 0.0.33 | Gap |
|---|---|---|
| tiles and trends | `StatCard` (trend, skeleton) · `Sparkline` (line, area, bar) · `MeterBar` (thresholds, for quota) · `HeatmapCalendarGrid` (uploads per day) | axis time-series chart and stacked bar: none; Wheelhouse drew an app-local `WeeklyBars`; fe `lib-adoption.md` lists Recharts as a companion |
| tables and filters | `Table*` · `DataTable` · `Pagination` · `SearchInput` · `SelectPicker` · `DateRangePicker` · `RelativeTime` · `TabsGroup` | cursor "load older" like Wheelhouse's audit panel |
| file rows | `ImagePreview` · `PdfViewer` · `VideoPlayer` · `AudioPlayer` · `FilePicker` · `foundation/uploads` queue | `fileUpload` is empty; `fe-files` list and grid, icons, share dialog are the P1 kit, not built |
| confirms and copy | `ConfirmPopover` · `TypedConfirmModal` · `CopyButton` | none |
| activity | `ActivityTimeline` for a file's audit tail | none |
| shell kit | `AppShell*` · `SidebarMenu*` · `PageHeader*` · `Breadcrumb` · `AccountMenu` · `AppSwitcherMenu` · `DataRegion` · `EmptyState` | `Panel`, `StatusPill`, `ScopeBar`, `ConsoleNav`, `RefreshButton` are Wheelhouse-local; Identity's admin console needs the same (U6) |

### 4.3 · Nav groups

```
Overview
Library   Files · Shares
Insight   Usage · Audit
Manage    Apps · Imports · Settings
Host      Tenants · Reports            (Shared, files.tenants)
profile ▸ System · File types · Sign out
```

- phone bottom bar: Overview, Files, Shares, Usage; the rest in a More sheet.
- the Overview item carries a dot while Attention has items.
- Pins as in Wheelhouse: the operator pins tenants; they sit under the switcher.

### 4.4 · Landing and tenant switcher

| Audience | Lands on | Scope bar |
|---|---|---|
| owner admin | Overview | instance name › page |
| operator | Overview, All tenants: per-tenant table in place of the app table | All tenants ▾ › page |
| tenant admin | Overview | tenant label (no switcher) › page |
| app developer | Apps | tenant › Apps › app |

- the switcher lists tenants with a state pill, pinned first, last choice remembered per browser.
- entering a tenant from All tenants writes one `operator.access` audit row per session and tenant; the tenant admin
  sees it in their Audit (U2).

### 4.5 · Empty states

| Page | Empty title | Offers |
|---|---|---|
| Overview, new instance | "Nothing stored yet" | base URL and audience with copy buttons; create a key; the `AddFilesClient` snippet |
| Overview, B without tenants | "No tenants yet" | create a tenant, or the config path while Tenants is read-only |
| Files | "No files" · filtered: "No files match" | upload; clear filters |
| Files › Trash | "Trash is empty" | the retention window |
| Files › Orphans | "No orphans" | what the sweep does and its grace window |
| Shares | "No shares" | a product or a file's Share action creates one |
| Usage | "Usage starts with the first upload" · series: "History starts {date}" | the date the snapshot began |
| Audit | "No activity in this range" | widen the range |
| Apps | "No apps yet" | apps appear on first upload or in policy config |
| Imports | "No imports" | Wheelhouse starts them; they show here |
| Tenants · Reports | "No tenants" · "No open reports" | create tenant |

- a 403 on one panel shows "This key cannot read this", never a hidden panel.
- every tile and table has a skeleton; every region has retry (`DataRegion`).

---

## 5 · Sign-in last

The console needs one thing from auth: a principal behind `ICurrentPrincipal` (T8 C1). The SPA never holds a token.

### 5.1 · The surface, the same before and after Identity

- `GET /console/context` returns the principal (`subject`, `kind`, scopes, tenant or tenants) and `signIn`, the list
  of ways to start a session: `key`, later `oidc`.
- a 401 sends the SPA to `/signin`; a 403 shows the denied notice.
- the session is an HttpOnly, SameSite=Strict cookie, host-only on the console host; mutations carry the antiforgery
  header (fe `foundation/http` `Antiforgery`).
- only `Files.Api` composes the scheme (`DevConsoleAuth`, `KeyConsoleAuth`, later `OidcConsoleAuth`); no package
  references Identity.

### 5.2 · Stopgap options

| Option | Gives | Cost | Risk |
|---|---|---|---|
| A · dev bypass | `Console:Auth=Dev`: a fixed principal with every scope | none | must never start outside Development; a startup check refuses |
| B · instance admin key | a hashed key in config or the secrets store, the same primitive as tenant keys (C9); `POST /console/session` swaps it for the cookie; rate-limited; audits `console.signin` with the key id | small | the key travels through a browser field; needs HTTPS and its own host |
| C · read-only keys | a key without `files.write` and `files.share` sees no mutating button; the server enforces | trivial on B | none |
| D · network gate | the console host answers on the tailnet only (the Wheelhouse console is tailnet-only, T9) | infra | none; pairs with B |
| E · wait for C10 | no console on B until Identity ships | none | blind operation of a host holding Ocharo's files |

- pick: A in development, B + C on every deployed instance (first production key read-only), D on B; E rejected.

### 5.3 · Scopes and what they open

| Scope | Console abilities |
|---|---|
| `files.read` | pages in read-only form, preview, download |
| `files.write` | upload, rename, delete, restore |
| `files.share` | create, edit, revoke shares |
| `files.admin` | Usage, Audit, Apps and policies, Imports (cancel), Settings (keys, hostnames) |
| `files.tenants` | Tenants, Reports, All-tenants scope, System on B |
| `files.delegate` | not used by the console |

The UI hides what a principal cannot do; the API re-checks every call.

### 5.4 · What C10 adds later

- the console becomes an OIDC BFF client: code + PKCE, back-channel logout, scopes `openid profile files.admin`
  (`files.tenants` for the operator) — T8 C10.
- P1: on B only the operator signs in, through WoW2's Identity; on an A the owner's admins sign in through the owner's.
- P2: a tenant's admins sign in through their own issuer. The scheme is picked at runtime
  (`IOidcSchemeResolver.ResolveAsync(tenant)`), so `/signin` needs the tenant first: the hostname, or a typed slug.
- the audit actor moves from a key id to (`iss`, `sub`) (C15); `org_role` can map to console roles, which makes the
  app developer audience real.
- the SPA does not change: `signIn` in the context gains `oidc`. The key form stays as the operator's break-glass
  behind the tailnet, or goes.
- the console host serves the console and the API only; content hosts (`files.{domain}`) never serve `/`, so the
  session cookie never shares an origin with user content.

---

## 6 · First console slice

Candidate **v0.2 · Console (read-mostly)**, right after v0.1; not in the version track.

- pages (7): Overview · Files · File detail · Usage · Audit · File types · Sign-in.
- why these: they need only what v0.1 stores (files, references, audit rows). No `Shared` mode, sharing, variants,
  quotas or Identity.
- actions: upload (under an app `console`), download and copy a signed URL, rename, delete, restore.
- left out on purpose: purge now and visibility flips (a public flip leaves year-long cached `/m/` copies), ZIP
  (needs archives), share creation, anything tenant-wide.
- one small addition: the Orphans view and its Attention count, since the sweep ships in v0.1 iteration 3.

Candidate iterations, in version-track phrasing:

1. Shell and session: open the console with an admin key; theme, nav, scope bar; `GET /console/context`.
2. Files: browse, filter, preview, download, rename, delete, restore; upload from the console.
3. Overview and Usage: tiles, Attention, by kind and app, largest, dedup, trash; the `storage_daily` snapshot starts here.
4. Audit and File types: filtered audit table with action counts; the registry grid.
5. Verification: read-only key shows no mutating button; a denied URL appears in Audit; Usage totals equal a hand count.

Needs from v0.1 and others:

- v0.1: the audit shape of § 3.3 (iteration 5), `orphaned_at` and the policy flag (iteration 3), the `files` indexes,
  the delivery counters (iteration 4).
- `Files.Api`: `GET /console/context`, console session, `GET /usage`, `GET /usage/daily`, `FileQuery` additions.
- `fe-files`: list and grid, icons, image and PDF preview, upload kit; `apps/web`: `themes.css`, app-local bars.

After it, by dependency:

| Slice | Pages added | Waits on |
|---|---|---|
| 2 · Sharing | Shares, Share detail, "Share" action in Files | the sharing feature |
| 3 · Host launch | Tenants, Tenant detail, Settings, Imports, All-tenants scope, System | `Shared` mode; Ocharo's cutover |
| 4 · P2 | Apps editor, quotas, Quarantine, Reports, webhooks, CSV export, tenant SSO | scanning, quotas, C10 P2 |

---

## 7 · Benchmarks

Search-based, 2026-10-27; `(U)` where a source did not show the dashboard itself.

| Console | Leads with | Borrow | Skip |
|---|---|---|---|
| [Cloudinary](https://cloudinary.com/documentation/billing_and_plans) | Dashboard: trend graphs of transformations, assets, storage, bandwidth; plan quota used; Reports for where credits go | tiles with trends; quota meter on top | credits |
| [ImageKit](https://help.imagekit.io/en/articles/2523397-tracking-real-time-usage-of-your-account) | bandwidth, requests, storage on every page; daily usage graphs; top assets; error report; savings graph | usage strip; top files (P2); rejections report; dedup saved as the savings figure | monthly bill estimate (billing is P3) |
| [Uploadcare](https://uploadcare.com/docs/keys) | per-project usage (uploads, traffic, storage, conversions); a project is an isolated environment with its own keys (U) | per-app usage with the same four figures | — |
| [Supabase](https://supabase.com/docs/guides/platform/manage-your-usage/storage-size) | usage page per organization with a project dropdown and a period; object counts need SQL | scope dropdown and period | making counts a query exercise |
| [Bunny](https://docs.bunny.net/reference/storagezonepublic_storagezonestatistics) | storage zone statistics: storage used by day, file count by day; egress apart | exactly those two series as Usage's default | — |
| [Backblaze B2](https://www.backblaze.com/docs/cloud-storage-create-and-manage-caps-and-alerts) | Buckets page (type, files, size, lifecycle); monthly transactions by class; Caps & Alerts at 75 % and 100 % of a daily cap | Apps table like the bucket list; audit action counts like transaction classes; quota alerts | per-class pricing |
| [S3 Storage Lens](https://docs.aws.amazon.com/AmazonS3/latest/userguide/storage_lens_view_metrics.html) | drill-down organization → account → region → class → bucket → prefix; 14 days free, 15 months advanced | drill order tenant → organization → app → kind; short live detail, long rollups | paid tiers |
| [Google Workspace admin](https://support.google.com/a/answer/4579578) | Apps usage report: storage per user against the allotment, users near their limit | a "near limit" list for organizations and apps (P2) | — |
| Dropbox admin, Drive audit log | not verified (U) | — | — |

- every one leads with usage and trend, not with a file browser; none lands on audit. Overview first holds.
- most have no notion of a reference; orphan accounting and dedup saved are ours to show.

---

## Points

Open decisions, parents before children. **F** = a fork only the developer can settle; **V** = a claim to validate.
Format: question → pick.

- [ ] U1 · V · one console app serves `Single` and `Shared` (nav and scope by mode and scopes), not two apps → one (§ 1)
- [ ] U2 · F · what the operator reaches on B: aggregates in All tenants, a tenant's file list after entering it (audited to the tenant), content only by break-glass → that (§ 4.4)
- [x] U3 · F · stats: live queries only, or nightly snapshot plus rollup from the first slice → rollup from day one, since storage history cannot be rebuilt (§ 3.1) — answered 2026-10-27: rollup from day one
- [ ] U4 · V · v0.1 iteration 5's audit row carries `outcome`, `error_code`, `app_id`, `client_id`, `bytes`, `hits`, one row per signed-URL redemption → yes, before the row is written (§ 3.3)
- [ ] U5 · F · sign-in before Identity: dev bypass plus an instance admin key with a cookie session, read-only first, behind the tailnet → that, over no console on B (§ 5.2)
- [ ] U6 · F · shell kit: lift Wheelhouse's `Panel`, `StatusPill`, `ScopeBar`, console nav into the fe sdk first, or copy them a third time → lift (§ 4.2)
- [ ] U7 · F · order after v0.1: the console slice (7 pages) before the Sharing slice → console first (§ 6)
- [ ] U8 · F · Tenants at B's launch: read-only over configured tenants (C14: P1 by configuration), or create, keys and suspend in the UI as the backlog row says → read-only first (§ 2)

### Self-served and deferred

- Overview is every audience's landing page, Attention first: all benchmarked consoles lead with usage and trend.
- app developer is a tenant admin filtered by app until Identity gives a per-app role.
- the console sits on its own host, never a content host: spec § Serve puts user content on its own origin.
- console uploads go under an app `console` whose policy skips the orphan sweep; otherwise each is an orphan.
- time series use `Sparkline` and app-local bars (Wheelhouse's `WeeklyBars`); an fe sdk chart companion waits for a second console that needs axes.
- the console watches and cancels imports; Wheelhouse starts them (W1).
- HTTP error rate and latency stay in OpenTelemetry and the future Monitoring app.
- deferred: audit retention and tamper evidence follow D14.
