# Prompt — orchestrator chat: Files with Identity, Ocharo and Wheelhouse

*Last updated: 2026-11-08*

> For a fresh chat started at the wow-two-ws root. Everything below the line is the prompt. That chat is the
> orchestrator: it starts four lane chats and is the only chat the developer writes to.

---

You orchestrate one goal across four repositories, by `~/.claude/conventions/agentic-workflow.md` § Orchestrated
sessions. This prompt is the developer's go-ahead to start the four lanes below in your first turn.

**Goal**

- WoW2's file server (Files, `wow-two-sdk-beta.files`) runs with Identity and serves Ocharo, deployed by Wheelhouse.
- Order: Identity is closed (2026-11-08), Files is next, then Events (developer, 2026-11-05).
- M1, analysis: every contract Files needs from Identity, Ocharo and Wheelhouse has a verdict (provided, change, add or
  drop) and a phase, and the developer has confirmed what each repository must do before Files P1.
- M2, build: starts only after the developer confirms M1, in the same lanes, against the track doc.
- M1 is done when the confirmed picks are written into Files' analysis or spec, each lane's backlog, and the track doc.

**How the developer works with you**

- The developer writes only to you. Lanes send their decisions to you by `SendMessage`.
- You ask the developer one decision per turn, naming the lane and quoting its question.
- You relay the developer's answer to the lane verbatim.
- You write no lane's code; you analyse only what no lane owns, with read-only agents.

**Lanes** (start each with `start_session`: `initiation: "user_asked"`, `use_worktree: false`, launched at the
wow-two-ws root, `"fresh"` context, a self-contained brief)

| Lane | Writes only | Slice |
|---|---|---|
| identity | `workbench/wow-two-sdk-beta/wow-two-sdk-beta.identity` | what Identity must add or change for Files, built as the whole vector |
| files | `workbench/wow-two-sdk-beta/wow-two-sdk-beta.files` | t8 against the settled rules and Identity as built; Files' side of every contract |
| platform | `workbench/mfts-projects/ventures/ocharo-hq/ocharo-ws/workbench/ocharo-platform` | Ocharo's adoption: which media moves, in which order, through which Files instance |
| wheelhouse | `workbench/wow-two-platform/wow-two-platform.wheelhouse` | deploying Files' instances, their settings and keys, and their registrations in each Identity |

- Each lane's first step verifies its slice of the snapshot below against its repository and reports the deltas
  before any analysis.
- The platform lane may read `ocharo-marketing` and `ocharo-studio` for their media, but writes neither.
- Each brief names the lane's agent candidates, its done-when with a pass/fail check, what it reports, and what it
  does on a blocker. It also says the orchestrator receives only the first ~400 characters of a lane's turn-end recap,
  so the lane leads with its report.
- Before starting a lane, check `list_sessions` for a live chat on its repository. `w2 - file server`
  (`local_292adfb3-8a44-47d6-9073-ebd751a9a2c9`, idle since 2026-11-03) holds the October Files analysis. It stays as
  history: start a fresh files lane, which reads that transcript only when needed.

**Track doc**

- Write it in the Files repo, under `engineering/planning/`.
- It holds the vectors, the lanes with their session names and ids, the order, the cross-lane contracts, the
  decisions and the done-when.
- Number its points as one `#N` series before any brief cites them.
- Commit it only while that repo's index is empty, since the files lane writes there too.

**Snapshot** (a lead; the repositories win where they disagree)

- Files' contracts, C1–C15 (2026-10-26): `workbench/wow-two-sdk-beta/wow-two-sdk-beta.files/engineering/research/files-analysis-2026-10/t8-identity-contracts.md`,
  with `t5-identity.md` (why) and `t1-services-tenancy.md` § 2 (instance kinds A and B). Files' spec is `product/product.md`.
  - Files pins be sdk `10.0.98-beta`, and its last commit before this prompt was on 2026-11-03.
- Settled rules that supersede parts of t8: `conventions/development/backend/dotnet/core/hla/api-communication.md`.
- Identity as built, at `3c11365`, released 2026-11-06:
  - Versions: image `v0.5.20`, NuGet `10.0.29-beta` and npm `0.0.15`.
  - Read first: `product/features/service-tokens.md` (its "Differences from T8" table) and `product/features/revocation-signals.md`.
- Ocharo:
  - Dev runs Identity `0.5.20` through platform `v0.1.27`, and prod is parked.
  - The `ocharo-platform` backlog row "Ocharo File Storage adoption" moves Catalogue, Brand, Blog and Marketing
    media, then Desk's `bytea` files.
  - Identity's closing record is `ocharo-platform` `engineering/planning/identity-cutover.md` #52–#54.
- Wheelhouse doesn't generate client keys yet (Identity's `service-tokens.md`). One vault holds both a client's secret
  and its key set.
- Leads from a read-only check on 2026-11-08 (code reading only; verify before relying on them):
  - Provided: C1 principal normalizer (the interface and `PrincipalKind` live in core), C3 resources with the audience
    from the registry, C4 client credentials with a secret or `private_key_jwt`, C6 token exchange with `act`, C8
    trusted issuers (`JwtOptions.TrustedIssuers`, `ITrustedIssuerResolver`), C15 the issuer on the principal.
  - Partly provided:
    - C2: `azp` became `client_id`, and the API-key principal carries no `iss`.
    - C7: no stated rule keeps a retired key in JWKS for its lifetime plus cache time, and the cache and refresh timings are unconfirmed.
    - C9: no `{partition}_{keyId}_{secret}` lookup; keys are found by hash only (`FindLiveByHashAsync`).
    - C10: the BFF exists, but there's no `IOidcSchemeResolver` for per-tenant schemes.
    - C12: only the session-ended and access-changed signals exist. They are JWT-signed and go to one URL per
      resource, and there's no `person.disabled` or `person.erasure_requested`.
    - C14: onboarding is configuration only, with no admin APIs.
  - Missing: C11 (`AllowGuests`, email-proof tokens) and C13 (`IAvatarStore`).
  - Superseded by `api-communication.md`:
    - C5's delegation headers and `files.delegate` give way to token exchange.
    - A person's call needs a scope plus a role or `org_permission`, not a scope alone.
    - t8's Standard Webhooks events give way to the two signals, sent through `Messaging/Webhooks`.

**Settled; don't reopen** (developer, 2026-11-05)

- Identity issues audience-bound tokens that services verify offline. A service acting as itself uses client
  credentials, and one acting for a person uses token exchange.
- Each caller gets one token per target, listing its scopes, and scopes are named `{service}.{action}`.
- Clients prove themselves with a secret by default, or a key set (`private_key_jwt`) by configuration.
- Revocation runs through the two signed signals. Long work is authorized when accepted, then runs as the service.
- API keys are only for callers outside Identity, such as Files' outside tenants.

**Constraints**

- SDK doctrine: Files' need triggers the vector, and the whole vector gets built (`CLAUDE.md` § SDK doctrine,
  `conventions/development/dev-cycle.md` § Vector completeness).
- One writer per repository; no worktrees. Each repository's git flags decide its commits, pushes and workflow runs,
  and builds run from the chat.
- Host addresses, tokens and phone numbers stay out of repositories and briefs.
- Out of scope: Events, Ocharo prod, and the Identity checks the developer parked (`ocharo-platform`
  `engineering/planning/backlog.md`, row "Dev sign-in checks").

**First reply**, after the four lanes are started:

- The lanes, each linked, with its session id.
- The t8 contracts that block Files P1 today, each with the repository that must change.
- One decision for the developer.
