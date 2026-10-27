# Files — full analysis before the build

*Last updated: 2026-10-27*

> The analysis batch behind the files domain repo (#3 in platform apps (`wow-two-ws/docs/platform-apps.md`)): how many
> services and instance kinds it needs, what products store today, the whole feature vector, the contract, what it
> needs from Identity, how wide the library is, and the icon pack. The [service spec](../../../product/product.md)
> stays the build source; settled findings move into it, and this folder keeps the evidence. Moved from the hub
> (`wow-two-ws/ideas/file-storage-analysis-2026-10/`) with the spec on 2026-10-26.

## Inputs

- spec: [product.md](../../../product/product.md) — D6 and D8 open; D7 decided 2026-10-26.
- identity analysis T4 (`wow-two-ws/ideas/identity-analysis-2026-10/t4-integration-modes.md`): File Storage is an API resource (M8),
  takes client-credentials tokens (M10), token exchange later (M11).
- domain structure (`wow-two-ws/conventions/development/repo/structure/domain-structure.md`): one repo per domain, packages
  by layer, the spec moves into the repo's `product/product.md`.
- developer, 2026-10-26: its own repo, separate from the be sdk; new libraries leave the sdk one domain at a time,
  bigger domains first.
- developer, 2026-10-26: beside one instance per owner, a second kind — a shared files host on a cheaper or
  storage-fit server that several products point at — to migrate files, and to start a product without a large server.
- developer, 2026-10-26: D7 — public delivery with gates (password, expiry, more); analyse every feature; the whole
  vector goes to the backlog; build what products need now, the rest in parallel after the first release.
- developer, 2026-10-26: an icon pack for every file extension; compression in scope; asked whether one big files
  library should hold the server as one of its parts.
- developer, 2026-10-26: the Identity chat is preparing the domain library template; this batch hands it Files' asks.
- developer, 2026-10-26 (second pass): every point A–F confirmed; the repo name reopened, since the domain is about
  media and files alike; Ocharo starts on B too; the first test migration runs through Wheelhouse, as a Wheelhouse
  module built from a prompt once its features, integrations and security are decided; the Identity contracts get
  analysed in full and the developer forwards a prompt to the Identity chat.

---

## Tracks

| # | Track | File | State |
|---|---|---|---|
| T1 | Services, roles and instance kinds (A owner, B shared host), tenant moves | [t1-services-tenancy.md](t1-services-tenancy.md) | ✅ |
| T2 | Feature vector: every capability, phased; sharing gates; compression | [t2-feature-vector.md](t2-feature-vector.md) | ✅ |
| T3 | Consumers: what products store today → the first slice | [t3-consumers.md](t3-consumers.md) | ✅ |
| T4 | Contract: brokers, HTTP API, hosting routes, events, errors | [t4-contract.md](t4-contract.md) | ✅ |
| T5 | Identity: what Files needs; asks for the Identity chat | [t5-identity.md](t5-identity.md) | ✅ |
| T6 | Library shape: one files domain, packages, what stays in core | [t6-library-shape.md](t6-library-shape.md) | ✅ |
| T7 | File icon pack | [t7-file-icons.md](t7-file-icons.md) | ✅ |
| T8 | Identity contracts C1–C15, with the prompt for the Identity chat | [t8-identity-contracts.md](t8-identity-contracts.md) · [prompt](prompt-identity-chat.md) | ✅ |
| T9 | Wheelhouse migration module: features, integrations, security, with a draft prompt for the Wheelhouse chat | [t9-wheelhouse-migration.md](t9-wheelhouse-migration.md) · [prompt draft](prompt-wheelhouse-migration.md) | ✅ |
| T10 | Console: audiences, pages, stats, shell, sign-in last, first slice; its decision points live in the track | [t10-console.md](t10-console.md) | ✅ |

---

## Points

The decision queue: parents before children, blocking first. Each line: the question → the pick, and the track
holding the evidence. Tick a line when the developer answers; the answer moves into the spec.

### A · Scope and placement — blocks the repo

- [x] Q1 (developer 2026-10-26) its own domain repo, not a be sdk module
- [x] Q2 (developer 2026-10-26: confirmed) one `files` domain — storage, server, formats, compression, images, documents, video, icons — with
  granular packages → yes (T6)
- [x] Q3 (developer 2026-10-26: `files`, after weighing media, assets, storage and drive) repo
  `wow-two-sdk-beta.files`; moved `Media.Images` and `Media.Pdf` namespaces become `Files.Images` and
  `Files.Documents`; the empty `wow-two-platform.storage.file` archived by the developer (T6)
- [x] Q4 (developer 2026-10-26: confirmed) core keeps `Storage/` byte adapters, since core's Messaging claim check reads them; Files builds on them → yes (T6)
- [x] Q5 (developer 2026-10-26: confirmed) `Media/Images` and `Media/Pdf` move to Files; Word, Markdown, Captions, Tabular stay in core → yes (T6)
- [x] Q6 (developer 2026-10-26: confirmed) amend domain structure § Scope: a stateless library moves with the domain whose pipeline uses it → yes (T6)

### B · Services and instances

- [x] Q7 (developer 2026-10-26: confirmed) one image, three roles (`api`, `worker`, `delivery`), one process in P1 → yes (T1)
- [x] Q8 (developer 2026-10-26: confirmed) B is the same image in `Tenancy:Mode=Shared`; A is the same with one tenant row → yes (T1)
- [x] Q9 (developer 2026-10-26: changed — Ocharo starts on B as well; it moves to its own A later, as the first test
  migration through Wheelhouse) WoW2's own instance is the first B (T1)
- [x] Q10 (developer 2026-10-26: confirmed) Form 3 becomes B, hosted off the control host → yes (T1)
- [x] Q11 (developer 2026-10-26: confirmed) tenant moves: signed export bundle + byte copy or bucket re-point; public URLs on the tenant's host → yes (T1)
- [x] Q12 (developer 2026-10-26: confirmed) B's server and byte store fold into D6 → yes (T1)

### C · Features and the first slice

- [x] Q13 (developer 2026-10-26: confirmed, Ocharo on B) P1 = Ocharo, Harvest captures, Events media, optionally
  ForeverPin logos, all on B; the rest to the backlog (T2, T3)
- [x] Q14 (developer 2026-10-26: confirmed) share gates in P1: expiry, password, max downloads, one-time, revoke; sign-in and email gates in P2 (T2)
- [x] Q15 (developer 2026-10-26: confirmed) user content on its own origin (`files.{domain}`) from P1 → yes (T2, T4)
- [x] Q16 (developer 2026-10-26: confirmed) on-the-fly transforms through signed params in P2, own Skia renderer, no imgproxy → yes (T2)
- [x] Q17 (developer 2026-10-26: confirmed) video posters in P2; transcoding and HLS in P3 on a named consumer → yes (T2)
- [x] Q18 (developer 2026-10-26: confirmed) lifecycle expiry and the orphan sweep move to P1 for Harvest → yes (T3)

### D · Contract

- [x] Q19 (developer 2026-10-26: confirmed) one broker set for Module and Client (`IFileUploads`, `IFileStore`, `IFileReferences`, `IFileUrls`,
  `IFileShares`, `IFileTransforms`, `IFileArchives`, `IFileJobs`, `IFileAdmin`) → yes (T4)
- [x] Q20 (developer 2026-10-26: confirmed) `FileId` UUIDv7, instance-independent; storage keys by content hash per tenant → yes (T4)
- [x] Q21 (developer 2026-10-26: confirmed) browsers upload through backend-minted tickets in P1; user tokens with the `files` audience in P2 → yes (T4, T5)
- [x] Q22 (developer 2026-10-26: confirmed) resumable uploads by tus 1.0 (tusdotnet) in P2 → yes (T4)

### E · Identity

- [x] Q23 (developer 2026-10-26: confirmed) Files uses our Identity (M8, M10; M11 in P2) and does not wait for it: P1 runs on existing client-credentials
  and API-key handlers → yes (T5)
- [x] Q24 (developer 2026-10-26: the developer forwards the prompt) hand the Identity chat the contracts C1–C15 of
  [T8](t8-identity-contracts.md) through [prompt-identity-chat.md](prompt-identity-chat.md) (T5, T8)

### F · Icons

- [x] Q25 (developer 2026-10-26: confirmed) one generated pack: ~24 kind glyphs + extension label, from one registry shared by be and fe → yes (T7)
- [x] Q26 (developer 2026-10-26: confirmed) no brand logos in P1 → yes (T7)

### G · Wheelhouse migration module — blocks the Wheelhouse prompt

- [x] W1 (developer 2026-10-26: W1–W9 confirmed) split: Files owns import, export and the CLI mover; Wheelhouse orchestrates; the product owns the manifest
  and its id-map migration → yes (T9)
- [x] W2 the mover runs on the source host and pushes straight to Files → yes (T9)
- [x] W3 manifest at `engineering/deployment/files-migration.yml` in the product repo → yes (T9)
- [x] W4 one generic data-move job, reused by Wheelhouse's planned server move → yes (T9)
- [x] W5 gates: dry run default, dev first, prod by the owner, retire separate with a final snapshot and a 7-day wait → yes (T9)
- [x] W6 Files-minted import tokens, tenant- and import-scoped, hours long, over stdin → yes (T9)
- [x] W7 Ocharo uses its own `files.{domain}` on B from day one → yes (T9)
- [x] W8 first run: Ocharo dev, Catalogue and Brand first, then prod → yes (T9)
- [x] W9 cancel and progress join Wheelhouse jobs as part of the module → yes (T9)

### Self-served

- ✅ D7 recorded as decided in platform apps (`wow-two-ws/docs/platform-apps.md`) § Decisions and removed from platform decisions
- ✅ spec updated with the settled points: own repo, shared host B, D7 scope, compression and icons in scope
- ✅ `catalog.yml` entry and the platform-apps row renamed to Files
- ✅ repo `wow-two-sdk-beta/wow-two-sdk-beta.files` created and scaffolded from the domain template (2026-10-26)
- ✅ this batch and the spec moved into the repo; hub copies deleted, inbound links updated (2026-10-26)
