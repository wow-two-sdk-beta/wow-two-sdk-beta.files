# Prompt — Wheelhouse chat: files migration module

*Last updated: 2026-10-26*

> Status: draft 2026-10-26; refined once the Files library's import/export API exists; the developer forwards it.
> Everything below the line is the prompt.

---

Files — our new files domain (store, share, process and serve files; repo `wow-two-sdk-beta.files`)
— needs a migration module in Wheelhouse. The developer wants the first test migrations to run through Wheelhouse:
Ocharo's legacy files into the shared Files host (B), later Ocharo's tenant from B to its own instance (A). The Files
analysis is done; the developer confirmed the nine points below on 2026-10-26. Build the module against them.

**Read first**

- `wow-two-ws/workbench/wow-two-sdk-beta/wow-two-sdk-beta.files/engineering/research/files-analysis-2026-10/t9-wheelhouse-migration.md` — the design source: what Wheelhouse has, the split, data path, manifest, job steps, security, gaps.
- `wow-two-ws/workbench/wow-two-sdk-beta/wow-two-sdk-beta.files/engineering/research/files-analysis-2026-10/t1-services-tenancy.md` § 2 instance kinds, § 4 moving a tenant.
- `wow-two-ws/workbench/wow-two-sdk-beta/wow-two-sdk-beta.files/engineering/research/files-analysis-2026-10/t4-contract.md` § 3 — the import and admin routes of the HTTP API.
- `t2-feature-vector.md` § 8 (same folder) — custom hostnames on B move to P1; cited by t9.

**What does not exist yet**

- The Files library was scaffolded on 2026-10-26: no import API, no export API, no `files` CLI image, no running instance.
- Build against the contract in t4 § 3 and t9 §§ 4–6; stub the Files side (a fake Files API, a fake mover image) until the library lands.
- In t4: `POST /imports`, `GET /imports/{id}` (scope `files.admin`), `POST /tenants/{id}/export` (scope `files.tenants`).
- In t9 only: `GET /imports/{id}/map`, a health check, `files migrate plan · push · verify`; t4 lists none of them yet.
- Request and response bodies, the export bundle format and CLI flags are not defined; write them as assumptions in your stub, never as fact.

**What Wheelhouse has** (t9 § 1; a read-only sweep of `wow-two-platform.wheelhouse` on 2026-10-26)

- Products, servers, targets (product × server × environment, `dev` and `prod`); a runner over SSH with no resident agent, whose deploy account may run `docker run` and `docker volume`.
- A pre-deploy snapshot on prod: `pg_dump` per database plus a volume tar, newest 5 kept.
- Detached jobs with a live step journal, resumable; owner allowlist, hash-chained audit, typed-ID prod gates, vault secrets over stdin.
- Ocharo is one product `ocharo` (4 repos, `ocharo_dev`, `ocharo_prod`); its file locations and runtime data volume are not modelled.
- Missing: data locations per product, a host-to-host mover, job cancel, file and byte progress.

**The split** (W1)

- Files owns the import and export APIs, the tenant export bundle, dedup, checksums, the id map and the `files` CLI image.
- Wheelhouse owns the job: plan, preflight, snapshot, mover run, freeze, cutover deploy, verify, rollback, retire, gates, audit.
- The product owns the manifest and the database migration that writes `file_id` from the id map.
- Wheelhouse never parses a product's files; migration semantics stay in Files so a product without Wheelhouse runs the same CLI by hand.

**Confirmed decisions**

- W1 — the split above.
- W2 — the mover runs on the source host and pushes straight to Files; nothing passes through the control host.
- W3 — the manifest sits at `engineering/deployment/files-migration.yml` in the product repo.
- W4 — one generic data-move job, reused by the planned server move (backlog v0.7).
- W5 — gates: dry run default, dev first, prod by the owner, retire separate with a final snapshot and a 7-day wait.
- W6 — credentials: Files-minted import tokens, tenant- and import-scoped, hours long, passed over stdin.
- W7 — Ocharo uses its own `files.{domain}` on B from day one; custom hostnames on B move to P1.
- W8 — first run: Ocharo dev, Catalogue and Brand first (D8 order), then prod.
- W9 — cancel and file and byte progress join Wheelhouse jobs as part of this module.

**What to build**

1. The manifest reader: `kind` per source, P1 kinds `postgres-column` and `volume-folder` (later `sqlite-column`, `s3-bucket`, `url-list`, `git-lfs`); every source names the reference each file gets. The t9 § 4 YAML is illustrative: its table, volume and column names are not Ocharo's.
2. The generic data-move job (W4), with the file payload first and a database-and-volume payload left open for the server move.
3. The mover run: `docker run` of the `files` image on the source host, volumes mounted `:ro`, the database through a read-only role that placement creates, never the product's own credentials.
4. Cancel and progress (W9): the mover reports counts and bytes into the step journal; a cancelled job leaves the source untouched and unfreezes.
5. The tenant-move variant: the same job with a Files source, B's signed export bundle into A's import, or a bucket re-point when Ocharo's bucket is its own (t1 § 4).

**The job steps** (t9 § 5)

| Step | Does | Reversible |
|---|---|---|
| 1 plan | reads the manifest, counts files and bytes per source, estimates time; a dry run by default | yes |
| 2 preflight | tenant exists, quota fits, credentials present, source reachable, Files healthy | yes |
| 3 snapshot | the existing pre-deploy snapshot of databases and volumes | yes |
| 4 copy | the mover uploads each file with its source key and SHA-256; idempotent per (tenant, source key) | yes |
| 5 freeze | upload paths go read-only for at most 10 minutes; the mover copies the delta | yes |
| 6 cutover | an ordinary deploy of the build that reads Files; its migration fills `file_id` from the id map | yes, redeploy the previous build |
| 7 verify | count and checksum parity per source; sample downloads through Files | yes |
| 8 unfreeze | uploads reopen, now into Files | yes |
| 9 retire | a separate later job: final snapshot, then drops the old columns and folders | no |

- The freeze lifts on its own when any later step fails; source data stays untouched until step 9.
- Retire needs a typed confirmation, a final snapshot and a 7-day wait; step-up 2FA joins once decision 66 lands.

**Security and gates**

- Who: owner allowlist only, the action header on every mutation, every step an audited command in the hash-chained trail.
- Import token: minted by Files for one tenant, one import, a few hours, scope `files.admin` limited to imports; it reaches the mover over stdin and never touches the target's disk.
- P1 asks Files for the token with an operator credential from the vault; P2 swaps it for Wheelhouse's own client-credentials token from Identity (C4).
- Integrity: SHA-256 computed at the source, checked by Files, compared again in verify.
- Privacy: logs carry counts and source keys only; TLS to Files, the tailnet when both hosts are on it.
- Agent policy: an agent may run dev migrations; an Ocharo prod migration stays the owner's to start, by typed target ID.

**Ocharo's first run** (W7, W8)

- Dev first, Catalogue and Brand before the other sources, then prod; every run starts as a dry run.
- Ocharo reaches B on its own `files.{ocharo domain}` from day one; Wheelhouse's domain binding is unbuilt, so P1 changes DNS by hand.
- B → A later: pause the tenant's uploads, move the bytes server to server, move the `files.{domain}` CNAME, B answers `308` for a grace window, then purges after its retention window.
- P2, not P1: the Identity token (C4), the CNAME switch from a domain binding, a Monitoring alert on a failed or stalled job.

**What to send back**

1. Job-model changes: the generic data-move job, cancel, progress, the freeze mechanism and where data locations live.
2. The manifest schema you settle, with the kinds and fields beyond the t9 sketch.
3. Every gap in the Files contract you hit while stubbing it: a missing route, field, error or scope.
4. What you could not verify in Wheelhouse, Ocharo's data volume included.

**Done when**

- A dry run and a full dev run of a sample manifest pass end to end against the stubbed Files side, every step in the journal.
- Cancel stops a running job and lifts the freeze; progress shows files and bytes per source.
- Retire runs as its own job behind its gates; the job takes its payload as a parameter, so the server move reuses it.
- The real Ocharo run waits for Files to ship its import API and `files` image; swapping the stub for it is the only change left.
