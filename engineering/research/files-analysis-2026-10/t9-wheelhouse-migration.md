# T9 — Wheelhouse migration module

*Last updated: 2026-10-26*

> The developer wants the first test migration — Ocharo's legacy files into B, later Ocharo's tenant from B to its own
> A — to run through Wheelhouse, as a Wheelhouse module built from a prompt once its features, integrations and
> security are decided. This track holds what Wheelhouse has, the split of work, the module's features, its
> integrations, its security, and the decisions the prompt waits on.

- Wheelhouse facts from a read-only sweep of `wow-two-platform.wheelhouse` on 2026-10-26; paths relative to its
  `engineering/codebase/`; `(U)` = unverified.

---

## 1 · What Wheelhouse has

| Area | Built | Missing for migrations |
|---|---|---|
| model | products, servers, targets (product × server × environment, `dev` and `prod`) | where a product's files live: DB columns and volume folders are not modelled |
| execution | `runner.py` streamed over SSH, no resident agent; the deploy account may run `docker run` and `docker volume` (`wheelhouse.runner-services/transport.py:95`) | a mover that pushes bytes host to host |
| data | prod pre-deploy snapshot: `pg_dump` per database + volume tar, newest 5 kept (`runner.py:856`, `:91`) | copy between hosts, off-host backup, verified restore |
| jobs | detached worker with a live step journal; re-run resumes at the first unfinished step | cancel, byte or file progress |
| security | GitHub owner allowlist, default-deny, action header on mutations, hash-chained audit of commands, typed-ID prod gates, vault secrets over stdin, tailnet-only console | step-up second factor (planned, decision 66) |
| Ocharo | managed as one product `ocharo` (4 repos, `ocharo_dev`, `ocharo_prod`), first deploy 2026-10-24 | its file locations; its runtime data volume (U) |

- planned and related: backlog "move a product to another server" — backup, restore, write freeze of 10 minutes or
  less, DNS switch, old host kept 7 days, step-up 2FA (v0.7); backup-only storage (decision 69); the "Move a workload"
  flow (`product/flows/flows.md`), described, not built.

---

## 2 · Who does what

| Party | Owns |
|---|---|
| Files | import and export APIs, the tenant export bundle, dedup, checksums, the id map, and a `files` CLI shipped as an image whose `migrate push` reads a source and uploads it |
| Wheelhouse | the job: plan, preflight, snapshot, run the mover on the source host, freeze, cutover deploy, verify, rollback, retire; gates and audit |
| the product | a migration manifest naming where its files live, and the database migration that writes `file_id` from the id map |

- migration semantics stay in Files, so a product without Wheelhouse can run the same CLI by hand.
- Wheelhouse stays a control plane: it never parses a product's files.

---

## 3 · Data path

| Option | Path | Verdict |
|---|---|---|
| through the control host | source host → SSH → control host → Files | double hop, files land near the console, slow |
| mover on the source host | Wheelhouse runs the `files` image on the source host with the volume mounted read-only and the product database reachable; it pushes straight to Files | **recommend**: one hop, no copy on the control host, uses `docker run` Wheelhouse already runs |
| Files pulls | Files reaches into the source host | needs inbound access to product hosts; refused |

---

## 4 · Manifest

The product declares its sources in its repo, beside the deploy descriptor, in
`engineering/deployment/files-migration.yml`; table, volume and column names below are illustrative, not Ocharo's:

```yaml
tenant: ocharo
sources:
  - id: desk-media
    kind: postgres-column          # bytes in a bytea column
    database: desk
    table: media_items
    key: id
    content: data
    name: file_name
    type: content_type
    reference: "desk.media:{id}"
  - id: catalogue-blobs
    kind: volume-folder            # content-hash folders
    volume: catalogue-data
    path: App_Data/blobs
    reference: "catalogue.blob:{path}"
```

- kinds in P1: `postgres-column`, `volume-folder`; later `sqlite-column`, `s3-bucket`, `url-list`, `git-lfs`.
- every source names the reference each file gets, so the product's records keep pointing at it after the move.

---

## 5 · The job

| Step | Does | Reversible |
|---|---|---|
| 1 plan | reads the manifest, counts files and bytes per source on the host, estimates time; a dry run by default | yes |
| 2 preflight | tenant exists, quota fits, credentials present, source reachable, Files healthy | yes |
| 3 snapshot | the existing pre-deploy snapshot of databases and volumes | yes |
| 4 copy | the mover uploads each file with its source key and SHA-256; idempotent per (tenant, source key), so a re-run resumes | yes |
| 5 freeze | the product's upload paths go read-only, at most 10 minutes; the mover copies the delta | yes |
| 6 cutover | deploys the product build that reads Files, whose migration fills `file_id` from the id map | yes, by redeploying the previous build |
| 7 verify | count and checksum parity per source; sample downloads through Files | yes |
| 8 unfreeze | uploads reopen, now into Files | yes |
| 9 retire | a separate later job: final snapshot, then drops the old columns and folders | **no** |

- the freeze unfreezes on its own when any later step fails.
- source data stays untouched until step 9, so rollback is always a redeploy.

### Tenant move between Files instances (B → Ocharo's A)

- the same job with a Files source: B's signed export bundle into A's import, or a bucket re-point when Ocharo's
  bucket is its own ([T1](t1-services-tenancy.md) § 4).
- cutover moves the `files.{ocharo domain}` CNAME; Wheelhouse's domain binding is unbuilt, so P1 changes DNS by hand.
- Ocharo therefore uses its own hostname on B from day one; [T2](t2-feature-vector.md) § 8 moves custom hostnames on
  B to P1.

---

## 6 · Integrations

| With | What | Phase |
|---|---|---|
| Files API | `POST /imports`, `GET /imports/{id}`, `GET /imports/{id}/map`, tenant export, health | P1 |
| Files CLI image | `files migrate plan · push · verify`, run by `docker run` on the source host | P1 |
| vault | per-product Files credentials and import tokens, passed over stdin | P1 |
| product repo | the manifest; the cutover build and its id-map migration | P1 |
| deploy | cutover is an ordinary deploy with the new build and `Files:*` settings | P1 |
| snapshots | step 3 and the final snapshot before retire | P1 |
| Identity | Wheelhouse's own client-credentials token for Files (C4) replaces the operator key | P2 |
| domains | CNAME switch for tenant moves | P2, by hand before |
| Monitoring | a failed or stalled job alerts | P2 |

---

## 7 · Security

- who: owner allowlist only, the action header on every mutation, every step an audited command in the hash-chained
  trail; Files audits each import on its side.
- import token: minted by Files for one tenant, one import and a few hours, scope `files.admin` limited to imports;
  it reaches the mover over stdin and never touches the target's disk.
- read-only source: volumes mounted `:ro`; the database through a read-only role that placement creates, not the
  product's own credentials.
- integrity: SHA-256 computed at the source, checked by Files, compared again in verify.
- privacy: bytes never pass through the control host; logs carry counts and source keys, never contents.
- transport: TLS to Files; on the tailnet when both hosts are on it.
- gates: dry run first; dev before prod; prod by typed target ID; retire as its own job with a typed confirmation, a
  final snapshot and a 7-day wait; step-up 2FA on retire once decision 66 lands.
- agent policy: an agent may run dev migrations; an Ocharo prod migration stays the owner's to start (Wheelhouse prod
  is the only product an agent promotes).

---

## 8 · Wheelhouse gaps the module closes

- cancel for jobs, and file and byte progress in the step journal.
- a generic data-move job — freeze, copy, verify, cutover, keep the old side, retire — that the planned server move
  (backlog, v0.7) reuses with a database-and-volume payload.
- data locations per product, read from the manifest.

---

## 9 · Decisions the Wheelhouse prompt waits on

| # | Question → pick |
|---|---|
| W1 | split: Files owns import, export and the CLI mover; Wheelhouse orchestrates; the product owns manifest and id-map migration → yes |
| W2 | data path: the mover runs on the source host and pushes straight to Files → yes |
| W3 | manifest at `engineering/deployment/files-migration.yml` in the product repo → yes |
| W4 | one generic data-move job, reused by the planned server move → yes |
| W5 | gates: dry run default, dev first, prod by the owner, retire separate with a final snapshot and a 7-day wait → yes |
| W6 | credentials: Files-minted import tokens, tenant- and import-scoped, hours long, over stdin → yes |
| W7 | Ocharo uses its own `files.{domain}` on B from day one; custom hostnames on B move to P1 → yes |
| W8 | first run: Ocharo dev, Catalogue and Brand first (D8 order), then prod → yes |
| W9 | add cancel and progress to Wheelhouse jobs as part of the module → yes |
