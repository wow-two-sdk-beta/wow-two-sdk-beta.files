# Files integration

*Last updated: 2026-11-08*

WoW2's file server (Files, this repo) runs with Identity and serves Ocharo, deployed by Wheelhouse. This is the goal's
cross-lane track: vectors, lanes, order, contracts, decisions and done-when. The orchestrator chat writes it; each lane
keeps its own backlog and version track in its own repo, and never edits this file.

Sources: [t8](../research/files-analysis-2026-10/t8-identity-contracts.md) (the contracts C1–C15),
[t5](../research/files-analysis-2026-10/t5-identity.md) (why),
[t1](../research/files-analysis-2026-10/t1-services-tenancy.md) (instance kinds A and B), [the spec](../../product/product.md),
`wow-two-ws` `conventions/development/backend/dotnet/core/hla/api-communication.md` (the settled rules), Identity's
`product/features/service-tokens.md` and `product/features/revocation-signals.md` at `3c11365`.

## Status

- ✅ M1 analysis: every lane wrote its picks (#33); #29 answered by the developer
- 🔄 M2 build: Files `v0.1.15` (`d07447a`) runs on `files-dev` on PostgreSQL with WebP variants; platform `v0.1.31` stores Brand's and Catalogue's media in Files
- ✅ Wave 1 (Brand, Catalogue) on dev, 2026-11-09:
  - dev held no media stored before Files (platform `2375a24`), so the three mover plans pass with 0 files (wheelhouse job `d1c6833d`)
  - Brand: a master logo uploaded by the Ocharo owner, release 1 published; anonymous `logo/social` and `logo/master` answer 302 to Ocharo's dev Files host and serve the uploaded SHA-256
  - Catalogue: test product `nimcha-v01` with one image; a member's `media/{sha}` answers 302 to Files and serves 144803 B, 1600×1200, the uploaded file's size
  - Catalogue release assets (platform `v0.1.32`, `369fa1d`): test catalogue `wave1-check` published as release `5c45c873`; the anonymous PDF download answers 302 with `no-store` to Files and serves `ocharo-wave1-check-en.pdf` (1 page), and the reader loads its page PNG from Files; the product page stayed out of export while `nimcha-v01` is a draft
  - fixed on the way: the trusted issuer typed without Identity's trailing `/` (wheelhouse); the mover's missing folder and closed WAL database (Files `v0.1.13`); deployed dev loading localhost links and the repo's local API key from `appsettings.Development.json` (Files `v0.1.15`)
- ✅ Wave 2 (Blog, Editorial, Marketing, Studio) on dev, 2026-11-09: marketing `v0.1.8`, studio `v0.1.12`, platform `v0.1.33`; Wheelhouse `v0.14.5` fixed a linked key hiding Brand's and Catalogue's own `Files:BaseUrl`
  - Blog serves Brand's logo by reference (#11), anonymous, through Files; an Editorial draft cover, a Marketing asset and a Studio look thumbnail each upload and read back through Files, the bytes matching
  - `marketing-assets` held 3 files on dev (5,066,568 B): import `01a1bf89-bf28-77da-995a-df0066ec4a16` moved them, verify matched 3 of 3 by SHA-256, and a `marketing` restart filled their file ids; a moved asset serves from Files (Wheelhouse `v0.14.6` fixed the imports path)
  - Studio's thumbnail framing fixed in studio `v0.1.13` (240×180); Studio, like the other SQLite apps, still stores data in SQLite, which `database.md:15` bans, and signs members out on restart: the Ocharo convention sweep carries both
- ✅ Wave 3 (Desk) on dev, 2026-11-09: platform `v0.1.34` (`2caf43b`), migration `012-files` applied at Desk's boot; the four `postgres-column` plans found 0 files
  - a preparation upload reads back with the same SHA-256; a sourcing upload redirects to Files, its thumbnail to the 480 px WebP variant
  - not yet read back: export download and commerce media, left for the cross-lane check
- ✅ Wave 4 (Assets) on dev, 2026-11-09: platform `v0.1.35` adds Identity client 7 `assets-release` (client credentials, `files.write`), its secret typed by the developer; `release-garments` uploaded 7 GLBs and the manifest (`bdb95c57…`), each served anonymously from `t/ocharo/m/{sha}`, and Studio's sync rewrote them byte-identical
- ✅ Cross-lane review, 2026-11-09: a fresh reviewer found 0 high, 0 medium, 4 low; all fixed (Files `v0.1.16` mover snapshot, Wheelhouse `639773d` token expiry, Catalogue deleted-file check `830e905`, tenant-prefixed garment links); the signal path is wired (`SignalUrl` matches, unsigned posts answer 401)
- ⬜ Next, in order (developer, 2026-11-09: "queue after we finish all remaining file server tasks - including updating the dbs"): the review fixes on dev; Ocharo's SQLite apps onto PostgreSQL (decision pending); the Ocharo convention sweep; then one real sign-out on dev proves the signal end to end, and the goal closes
- ⬜ After the goal closes, sweep included (developer, 2026-11-09): Files' whole-vector pass over its P1 backlog
- Every lane records what it builds in its repos' active version track (developer, 2026-11-09)
- ✅ Identity's P1 gate released (#41): `0655603`, NuGet `10.0.30-beta` on nuget.org, image `v0.5.21`, npm `0.0.15` unchanged (2026-11-08); Files re-pins `Identity.Auth` and `Identity.Testing` to `10.0.30-beta`
- ✅ Ocharo's Identity registers Files from platform `v0.1.28` (`8cff011`); its 2 owner keys on `ocharo-dev` wait for the wheelhouse lane's console access

## Goal

| Milestone | Done when |
|---|---|
| M1 analysis | every contract #8–#22 has a verdict (provided, change, add or drop) and a phase, and each lane has written its picks into Files' t8 or spec, its backlog and this doc; open decisions go to the developer (#33) |
| M2 build | each repo's P1 rows from M1 are built, released and deployed to dev; Files serves Ocharo's dev media through the instance #7 names |

- Pass/fail for M1: each of #8–#22 shows a verdict and phase in the contract table below, and each lane's
  backlog cites the `files-integration #N` points it owns.
- Out of scope: Events, Ocharo prod, and the Identity checks the developer parked (`ocharo-platform`
  `engineering/planning/backlog.md`, row "Dev sign-in checks").

## Lanes

| Lane | Session | Writes only | Slice |
|---|---|---|---|
| identity | `fs-identity`, `local_0c946875-f69b-41dc-9a02-5f1f96280bd6` | `workbench/wow-two-sdk-beta/wow-two-sdk-beta.identity` | what Identity must add or change for Files, built as the whole vector (#25) |
| files | `fs-files`, `local_de3cbee3-34cc-4f86-a7a5-b5a540099ac0` | `workbench/wow-two-sdk-beta/wow-two-sdk-beta.files` | t8 against the settled rules and Identity as built; Files' side of every contract (#26) |
| platform | `fs-platform`, `local_008e0cad-f745-41e8-9b9e-e943aedee76e` | every Ocharo repo under `workbench/mfts-projects/ventures/ocharo-hq/ocharo-ws/workbench/`: `ocharo-platform`, and since 2026-11-09 `ocharo-marketing`, `ocharo-studio`, `ocharo-assets` (#42) | Ocharo's adoption: which media moves, in which order, through which Files instance (#27) |
| wheelhouse | `fs-wheelhouse`, `local_65891b71-fc11-4074-8936-19f3f7b32d9e` | `workbench/wow-two-platform/wow-two-platform.wheelhouse` | Files' instances, their settings and keys, their registrations in each Identity (#28) |

- The orchestrator is `WoW2 Files orchestration across four repos` (`local_0ecb6944-12ce-4bab-8083-0dc5ffc2d562`).
- The October Files analysis sits in `w2 - file server` (`local_292adfb3-8a44-47d6-9073-ebd751a9a2c9`), kept as history.
- The developer writes only to the orchestrator; lanes send their decisions there by `SendMessage`.

## Order

1. Each lane verifies its slice of the snapshot against its repo and reports the deltas (#24).
2. Each lane gives a verdict and a phase for the contracts it owns; the orchestrator asks the developer only the open decisions, one per turn.
3. Picks go into Files' t8 or spec, each lane's backlog and this doc: M1 closes per lane, and its M2 build starts (#33).
4. M2: identity releases first (NuGet, npm, image); files re-pins and builds; platform registers and adopts; wheelhouse deploys.
5. The developer reviews the built result at the end and asks for changes then (#33).

## Settled

Developer, 2026-11-05 and 2026-10-26; not reopened.

- #1 Identity issues audience-bound tokens that services verify offline: client credentials when a service acts as itself, token exchange when it acts for a person.
- #2 Each caller gets one token per target, listing its scopes; scopes are named `{service}.{action}`.
- #3 Clients prove themselves with a secret by default, or a key set (`private_key_jwt`) by configuration.
- #4 Revocation runs through the two signed signals; long work is authorized when accepted, then runs as the service.
- #5 API keys serve only callers outside Identity, such as Files' outside tenants.
- #6 Order: Identity closed on 2026-11-08, Files is next, then Events (developer, 2026-11-05).
- #7 Ocharo starts as a tenant on the shared files host B and moves to its own instance later (`product/product.md`, 2026-10-26).
- #33 The developer answers only the decisions the goal needs and reviews the built result at the end (2026-11-08: "just ask things what you need from me, we'll review what you build and jow we need to change it at the end only"). Picks no longer wait on confirmation; each lane builds once its picks are written, and only work tied to an open decision waits.

## Contracts

Lead verdicts come from a read-only code check on 2026-11-08 and stay leads until the owning lane verifies them. The
`Confirmed` column holds the owning lane's verified pick (#33).

| # | Contract | t8 phase | Lead verdict | Repo that changes | Confirmed |
|---|---|---|---|---|---|
| #8 | C1 principal seam | P1 | provided: `ICurrentPrincipal` and `PrincipalKind` in core `Abstractions/Identity/CurrentPrincipal`; Identity's `ClaimsPrincipalNormalizer` reads `azp` and `client_id`; Identity pins core `10.0.103-beta` | files adopts: `ICurrentPrincipal` first ships in be `10.0.99-beta`, and Identity's `Auth` sets the floor at `10.0.103-beta` (files lane, 2026-11-08) | — |
| #9 | C2 claim vocabulary | P1 | change: `azp` became `client_id`; the API-key principal carries no `iss` | files adopts `client_id`; identity stamps `iss` on keys | — |
| #10 | C3 resources and scopes | P1 | provided: `Identity:Server:Resources` list, audience from the registry, URI with its trailing slash | files names its scopes; registrations per #32 | — |
| #11 | C4 client credentials | P1 | provided: secret or `private_key_jwt` | platform registers Ocharo's clients | — |
| #12 | C5 delegation headers | P1 | drop: token exchange replaces the headers and `files.delegate` | files | — |
| #13 | C6 token exchange with `act` | P2 | provided; moves to P1, since it replaces #12 | files accepts `act`; platform grants `token_exchange` | — |
| #14 | C7 discovery, JWKS, rotation | P1 | change: no stated rule keeps a retired key for token lifetime plus cache time; cache and refresh timings unconfirmed | identity; wheelhouse runs rotation | — |
| #15 | C8 multi-issuer validation | P1 on B | provided: `JwtOptions.TrustedIssuers`, `ITrustedIssuerResolver` | files implements the resolver over its tenants | — |
| #16 | C9 API keys | P1 on B | change: `ApiKeyRecord` already carries client, organization, partition and expiry, and the handler emits them; the gaps are the `{partition}_{keyId}_{secret}` lookup and `iss` (identity lane, 2026-11-08); Files needs no prefix, since a hash lookup on one table finds the tenant (files lane, 2026-11-08) | identity (`packages/Auth`); files implements the repository | — |
| #17 | C10 console sign-in | P1 operator, P2 tenants | change: the BFF exists, no `IOidcSchemeResolver` for per-tenant schemes | identity; the P1 issuer per #29 | — |
| #18 | C11 share gate, guest email proof | P2 | add: no `AllowGuests`, no email-proof tokens | identity | — |
| #19 | C12 events | P2 | change: the two signals replace t8's events, sent through `Messaging/Webhooks`; no `person.disabled` or `person.erasure_requested` | files receives signals; identity adds the two events | — |
| #20 | C13 avatar port | Identity P2 | change: `IProfilePictureStore` (keyed by `Guid`, default `EfProfilePictureStore`) sits in `packages/Module`; lifting it to Abstractions lets Files ship an adapter (identity lane, 2026-11-08) | identity | — |
| #21 | C14 tenant onboarding on B | P1 | change: configuration only, no admin APIs | wheelhouse and platform configure; identity and files add admin APIs | — |
| #22 | C15 issuer on the principal | P1 | provided: Files keys people by (`iss`, `sub`) | files | — |

- #23 A person's call needs a scope plus a role or `org_permission`, never a scope alone (`api-communication.md`); Files' policies change to match.
- #35 B's audience is each tenant's own `https://files.{domain}/`, with the slash, checked per issuer through `TrustedIssuer.Audiences`; a move from B to A is a DNS change (files lane D1 (a), orchestrator pick 2026-11-08 under #33).
- #36 A person's right to act comes from `org_permission`: each tenant's Identity grants `files:read`, `files:write`, `files:share` and `files:admin` to its roles, and Files reads only those (files lane D2 (a), orchestrator pick 2026-11-08 under #33).
- #37 Files' P1 needs nothing new from Identity: no `iss` on key principals and no key prefix. Its pins are be `10.0.103-beta` or later, plus Identity `Auth` and `Testing` at `10.0.29-beta`. Its P2 asks to Identity are an organization-deleted event, an erasure event distinct from `Removed`, and a signal receiver over `ITrustedIssuerResolver`; `person.disabled` drops (files lane, 2026-11-08).
- #41 Identity's gates before Files P1, released first (identity lane, 2026-11-08): G1 (#19) the signal receiver honours `ITrustedIssuerResolver`, since `IdentitySignalSignatureValidator.cs:61` builds with `hasResolver: false` and refuses signals from a runtime-added issuer; G2 (#14, #9) the rotation rule written into `deployment.md` (24 h pre-publish and 24 h retire cover a 12 h validator cache and 10-minute tokens), t6's timings fixed, and `client_id` on service tokens pinned by a test; G3 (#16, #9) `iss` on the key principal and a checksummed key text, only when #29 leaves WoW2 callers on API keys. API key text gains a GitHub-style checksum, lookup stays by hash (identity lane's pick under #33). No core change is needed (#31). Every other Identity change is P2.
- #42 Ocharo adopts Files in 4 waves: 1 Brand and Catalogue (`ocharo-platform`), 2 Blog, Marketing and Studio (`ocharo-marketing`, `ocharo-studio`), 3 Desk (`ocharo-platform`), 4 Assets releases (`ocharo-assets`); plan in `ocharo-platform` `engineering/planning/files-adoption.md` (`afacc36`). Waves 2 and 4 go to the platform lane too, since the waves run in series (developer, 2026-11-09: "I think one chat should handle all ocharo work - even tho they are in different repos"); a repo splits off to its own chat only when it must run in parallel or the lane grows too much. Superseded: one lane per repo, proposed once wave 1 read back (orchestrator pick 2026-11-08 under #33).
- #34 t8's "exists today" column is stale: Files hand-rolls C8 and C9 in `Files.Api/Auth` (JWT over `Files:Auth:Issuers` with a `files_tenant` claim, API keys against config hashes); M2 swaps them for Identity's `WoW2.Sdk.Beta.Be.Identity.Auth`, which the one-way rule allows (files lane, 2026-11-08).

## Lane slices

- #24 First step of every lane: verify its slice of the snapshot against its repo and report the deltas to the orchestrator before any analysis.
- #25 identity: verdicts and phases for #9, #14, #16–#21 on Identity's side; confirms #8, #10, #11, #13, #15 and #22 as provided; the whole vector in M2.
- #26 files: Files' side of #8–#23; rewrites t8 against the settled rules once M1 is confirmed; the be sdk and Identity pins Files needs.
- #27 platform: which Ocharo media moves (Catalogue, Brand, Blog, Marketing, then Desk's `bytea` files), in which order, through which Files instance; Ocharo's clients and grants for Files.
- #28 wheelhouse: where Files' instances deploy, their settings, secrets and keys, their registrations in each Identity, and the rotation procedure for #14.

## Open

- #29 Answered by the developer, 2026-11-08: "it's the least priority, the goal of this chat is to complete file server not wow2 identity". B trusts only Ocharo's Identity in P1; no WoW2 Identity instance in this goal; #17 takes its no-instance branch, and Identity's G3 (#41) is no P1 gate. The question as asked: B's issuers in P1: WoW2 probably runs no Identity instance of its own (catalog `identity` row: `deploy: none`), so t8's operator sign-in through WoW2's Identity (#17) and WoW2 callers on B have no issuer yet. Asked of the developer 2026-11-08. Identity's `engineering/deployment/deployment.md` plans WoW2's shared instance as a Wheelhouse product of its own; its `deploy.yml` lacks the `generate` on the 3 certificates that Ocharo's pin has (wheelhouse lane, 2026-11-08). Sizing: a WoW2 Identity on dev needs no Wheelhouse code, about a day of ops and ~512 MB of memory; the identity lane adds `generate` on its 3 certificates and `from: Sites:app:Url` on `Issuer` in one release; WoW2's registrations have no repo home yet. Without it, B trusts only Ocharo's Identity in P1 and operator sign-in waits for P2.
- #30 The `ocharo-platform` backlog row "Ocharo File Storage adoption" (`9bd3a24`, 2026-10-21) names "the Ocharo instance"; #7 (2026-10-26) puts Ocharo on B first. The platform lane updates the row.
- #31 Core changes: no lane writes the core be sdk; a core change a lane needs (#8) goes to the orchestrator as a blocker.
- #32 Registrations: which repo holds each Identity instance's `Identity:Server:Resources` and `Clients` entries for Files; the platform and wheelhouse lanes settle it between them through the orchestrator. Settled 2026-11-08, both lanes proposing the same split: `ocharo-platform` `engineering/deployment/deploy.yml` holds `Identity:Server:Resources:2` (`files`) and each Ocharo client's Files grants, resources and scopes; Wheelhouse types `Resources:2:Uri` (`https://files.{ocharo domain}/`, #35) and `SignalUrl` (that `Uri` plus `signals/identity`) on target `ocharo-dev`, and the same `Uri` again as the `ocharo` tenant's audience on `files-dev`; client secrets link inside product `ocharo` with `from`; no Wheelhouse code in P1, and a cross-product `from` is a P2 Wheelhouse row.
- #38 The t9 migration module builds a dev slice in P1: plan, preflight, dry run, the mover on the source host, verify, the id-map hand-off, cancel and progress. Freeze, cutover, rollback and retire come with Ocharo prod in P2, since prod is out of scope (wheelhouse lane option 1, orchestrator pick 2026-11-08 under #33).
- #39 B deploys for P1 as target `files-dev` on the products host beside `ocharo-dev`, its bytes on the `data` volume until D6 picks a store; the later move to a storage-fit server is a tenant move (wheelhouse lane option 1, orchestrator pick 2026-11-08 under #33).
- #40 Host aliases (several hosts per Wheelhouse site) are a P2 gate before a second tenant or B's default host; in P1 Ocharo's Files hostname is the `app` site's one named host, its DNS record set by hand. Key rotation (#14) and client key pairs are P2, due before Ocharo dev's certificate ends on 2027-02-28 (wheelhouse lane, 2026-11-08).

## Cross-lane contracts

| From | To | What | When |
|---|---|---|---|
| identity | files, platform | released NuGet, npm and image versions carrying #9, #14, #16 | M2, first |
| files | wheelhouse | Files' `engineering/deployment/deploy.yml`: service, settings, secrets, signal URL | M2 |
| files | platform, wheelhouse | Files' resource name, URI form and scopes for each Identity's registry (#10) | M1 |
| platform | files | Ocharo's media kinds, sizes and order (#27) | M1 |
| platform | platform | Ocharo clients' secrets for Files, linked with `from` inside product `ocharo`; no Wheelhouse work (wheelhouse lane, 2026-11-08) | M2 |
| files, platform | wheelhouse | Files' trusted issuer on B and Files' `Uri` and `SignalUrl` in Ocharo's Identity, typed per environment: `from` links only inside one product | M2 |
| files | wheelhouse | the `deploy.yml` key names for a tenant on B: slug, trusted issuer, metadata, audience, hostnames, byte store | M2 |
| files | wheelhouse | the `files` CLI image's `migrate plan`, `push` and `verify` (arguments, stdin, exit codes, where the image is published), import-token minting, and the `POST /api/files/v1/imports`, `GET …/imports/{id}` and `GET …/imports/{id}/map` shapes, each answer inside `{ "data": … }` (t9 wrote `/imports`; Wheelhouse `28d6d0f` fixed the 404 that caused, 2026-11-09) | M2 |
| platform | wheelhouse | the path and schema of `engineering/deployment/files-migration.yml`, or a yes that t9 § 4 is the schema as written | M2 |
| platform | files | yes or a correction on P1 WebP variants (480, 900, 1200, 1400 px), public `/m` on a tenant's hostname, cross-app references in one tenant, and no `sqlite-column` source in P1; plan in `ocharo-platform` `engineering/planning/files-adoption.md` (`afacc36`) | M1 |
| wheelhouse | platform | B on `files-dev` with tenant `ocharo`, Ocharo's dev hostname and bucket, before platform adds Files' registry entries: a required untyped key refuses every Identity deploy | M2 |
