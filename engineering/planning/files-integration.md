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

- 🔄 M1 analysis: lanes started 2026-11-08; each lane verifies its slice of the snapshot first (#24)
- ⬜ M2 build: starts only after the developer confirms M1, in the same lanes, against this doc

## Goal

| Milestone | Done when |
|---|---|
| M1 analysis | every contract #8–#22 has a verdict (provided, change, add or drop) and a phase, the developer has confirmed what each repo must do before Files P1, and the confirmed picks are written into Files' t8 or spec, each lane's backlog and this doc |
| M2 build | each repo's P1 rows from M1 are built, released and deployed to dev; Files serves Ocharo's dev media through the instance #7 names |

- Pass/fail for M1: each of #8–#22 shows a confirmed verdict and phase in the contract table below, and each lane's
  backlog cites the `files-integration #N` points it owns.
- Out of scope: Events, Ocharo prod, and the Identity checks the developer parked (`ocharo-platform`
  `engineering/planning/backlog.md`, row "Dev sign-in checks").

## Lanes

| Lane | Session | Writes only | Slice |
|---|---|---|---|
| identity | `fs-identity`, `local_0c946875-f69b-41dc-9a02-5f1f96280bd6` | `workbench/wow-two-sdk-beta/wow-two-sdk-beta.identity` | what Identity must add or change for Files, built as the whole vector (#25) |
| files | `fs-files`, `local_de3cbee3-34cc-4f86-a7a5-b5a540099ac0` | `workbench/wow-two-sdk-beta/wow-two-sdk-beta.files` | t8 against the settled rules and Identity as built; Files' side of every contract (#26) |
| platform | `fs-platform`, `local_008e0cad-f745-41e8-9b9e-e943aedee76e` | `workbench/mfts-projects/ventures/ocharo-hq/ocharo-ws/workbench/ocharo-platform` | Ocharo's adoption: which media moves, in which order, through which Files instance (#27) |
| wheelhouse | `fs-wheelhouse`, `local_65891b71-fc11-4074-8936-19f3f7b32d9e` | `workbench/wow-two-platform/wow-two-platform.wheelhouse` | Files' instances, their settings and keys, their registrations in each Identity (#28) |

- The orchestrator is `WoW2 Files orchestration across four repos` (`local_0ecb6944-12ce-4bab-8083-0dc5ffc2d562`).
- The October Files analysis sits in `w2 - file server` (`local_292adfb3-8a44-47d6-9073-ebd751a9a2c9`), kept as history.
- The developer writes only to the orchestrator; lanes send their decisions there by `SendMessage`.

## Order

1. Each lane verifies its slice of the snapshot against its repo and reports the deltas (#24).
2. Each lane gives a verdict and a phase for the contracts it owns; the orchestrator asks the developer one decision per turn.
3. Confirmed picks go into Files' t8 or spec, each lane's backlog and this doc: M1 closes.
4. M2: identity releases first (NuGet, npm, image); files re-pins and builds; platform registers and adopts; wheelhouse deploys.

## Settled

Developer, 2026-11-05 and 2026-10-26; not reopened.

- #1 Identity issues audience-bound tokens that services verify offline: client credentials when a service acts as itself, token exchange when it acts for a person.
- #2 Each caller gets one token per target, listing its scopes; scopes are named `{service}.{action}`.
- #3 Clients prove themselves with a secret by default, or a key set (`private_key_jwt`) by configuration.
- #4 Revocation runs through the two signed signals; long work is authorized when accepted, then runs as the service.
- #5 API keys serve only callers outside Identity, such as Files' outside tenants.
- #6 Order: Identity closed on 2026-11-08, Files is next, then Events (developer, 2026-11-05).
- #7 Ocharo starts as a tenant on the shared files host B and moves to its own instance later (`product/product.md`, 2026-10-26).

## Contracts

Lead verdicts come from a read-only code check on 2026-11-08 and stay leads until the owning lane verifies them. The
`Confirmed` column fills only on the developer's answer.

| # | Contract | t8 phase | Lead verdict | Repo that changes | Confirmed |
|---|---|---|---|---|---|
| #8 | C1 principal seam | P1 | provided: `ICurrentPrincipal` and `PrincipalKind` in core | files adopts | — |
| #9 | C2 claim vocabulary | P1 | change: `azp` became `client_id`; the API-key principal carries no `iss` | files adopts `client_id`; identity stamps `iss` on keys | — |
| #10 | C3 resources and scopes | P1 | provided: `Identity:Server:Resources` list, audience from the registry, URI with its trailing slash | files names its scopes; registrations per #32 | — |
| #11 | C4 client credentials | P1 | provided: secret or `private_key_jwt` | platform registers Ocharo's clients | — |
| #12 | C5 delegation headers | P1 | drop: token exchange replaces the headers and `files.delegate` | files | — |
| #13 | C6 token exchange with `act` | P2 | provided; moves to P1, since it replaces #12 | files accepts `act`; platform grants `token_exchange` | — |
| #14 | C7 discovery, JWKS, rotation | P1 | change: no stated rule keeps a retired key for token lifetime plus cache time; cache and refresh timings unconfirmed | identity; wheelhouse runs rotation | — |
| #15 | C8 multi-issuer validation | P1 on B | provided: `JwtOptions.TrustedIssuers`, `ITrustedIssuerResolver` | files implements the resolver over its tenants | — |
| #16 | C9 API keys | P1 on B | change: no `{partition}_{keyId}_{secret}` lookup, keys found by hash only (`FindLiveByHashAsync`) | identity (`packages/Auth`); files implements the repository | — |
| #17 | C10 console sign-in | P1 operator, P2 tenants | change: the BFF exists, no `IOidcSchemeResolver` for per-tenant schemes | identity; the P1 issuer per #29 | — |
| #18 | C11 share gate, guest email proof | P2 | add: no `AllowGuests`, no email-proof tokens | identity | — |
| #19 | C12 events | P2 | change: the two signals replace t8's events, sent through `Messaging/Webhooks`; no `person.disabled` or `person.erasure_requested` | files receives signals; identity adds the two events | — |
| #20 | C13 avatar port | Identity P2 | add: no `IAvatarStore` | identity | — |
| #21 | C14 tenant onboarding on B | P1 | change: configuration only, no admin APIs | wheelhouse and platform configure; identity and files add admin APIs | — |
| #22 | C15 issuer on the principal | P1 | provided: Files keys people by (`iss`, `sub`) | files | — |

- #23 A person's call needs a scope plus a role or `org_permission`, never a scope alone (`api-communication.md`); Files' policies change to match.

## Lane slices

- #24 First step of every lane: verify its slice of the snapshot against its repo and report the deltas to the orchestrator before any analysis.
- #25 identity: verdicts and phases for #9, #14, #16–#21 on Identity's side; confirms #8, #10, #11, #13, #15 and #22 as provided; the whole vector in M2.
- #26 files: Files' side of #8–#23; rewrites t8 against the settled rules once M1 is confirmed; the be sdk and Identity pins Files needs.
- #27 platform: which Ocharo media moves (Catalogue, Brand, Blog, Marketing, then Desk's `bytea` files), in which order, through which Files instance; Ocharo's clients and grants for Files.
- #28 wheelhouse: where Files' instances deploy, their settings, secrets and keys, their registrations in each Identity, and the rotation procedure for #14.

## Open

- #29 B's issuers in P1: WoW2 probably runs no Identity instance of its own (catalog `identity` row: `deploy: none`), so t8's operator sign-in through WoW2's Identity (#17) and WoW2 callers on B have no issuer yet. Asked of the developer 2026-11-08.
- #30 The `ocharo-platform` backlog row "Ocharo File Storage adoption" (`9bd3a24`, 2026-10-21) names "the Ocharo instance"; #7 (2026-10-26) puts Ocharo on B first. The platform lane updates the row.
- #31 Core changes: no lane writes the core be sdk; a core change a lane needs (#8) goes to the orchestrator as a blocker.
- #32 Registrations: which repo holds each Identity instance's `Identity:Server:Resources` and `Clients` entries for Files; the platform and wheelhouse lanes settle it between them through the orchestrator.

## Cross-lane contracts

| From | To | What | When |
|---|---|---|---|
| identity | files, platform | released NuGet, npm and image versions carrying #9, #14, #16 | M2, first |
| files | wheelhouse | Files' `engineering/deployment/deploy.yml`: service, settings, secrets, signal URL | M2 |
| files | platform, wheelhouse | Files' resource name, URI form and scopes for each Identity's registry (#10) | M1 |
| platform | files | Ocharo's media kinds, sizes and order (#27) | M1 |
| wheelhouse | platform | Ocharo clients' secrets for Files, linked with `from` | M2 |
