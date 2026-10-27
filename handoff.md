# Handoff — Files, local chat → cloud chat

*Written 2026-10-27*

> A courier for one reader: the cloud chat that continues Files. On load, move anything durable into the version doc,
> the backlog or the architecture docs, then delete this file and commit the deletion. The version doc outranks it.

## Load

1. `CLAUDE.md`, then `product/context.md` (state, tasks, decisions).
2. `engineering/planning/version-track/v0.1/v0.1.md` — the active version.
3. `engineering/planning/backlog.md`, then the analyses that pick the next slice:
   `engineering/research/files-analysis-2026-10/t10-console.md` § 6 and `t11-next-and-shares.md`.
4. Conventions: the repo docs cite `wow-two-ws/conventions/…`, which this checkout lacks. Clone the public hub
   `https://github.com/wow-two/wow-two-ws` read-only beside the repo and open `conventions/conventions.md` (the index)
   before touching code, structure or planning. Its `workbench/` is empty: managed repos are not in it.

## State

- v0.1 File store: iterations 1–7 shipped and pushed; Iteration 8 (Verification) ticks only on the developer's word.
- Image `v0.1.7` released; build, CodeQL and conformance against the image are green.
- Tests: backend 192 (Unit 83, Integration 35, Conformance 32, E2E 40, Migrations 2); frontend 20.
- Pins: be sdk `10.0.88-beta` (`BackendSdkVersion`), fe sdk `0.0.32`.
- Packages: NuGet policy, npm `0.0.1`, npm trust and `PUBLISH_PACKAGES=true` are set. Auto mode refused the agent's
  dispatch, so the developer runs the first publish:
  `gh workflow run publish-packages.yml -R wow-two-sdk-beta/wow-two-sdk-beta.files --ref main`. npm drops the trust
  with no publish by 2026-10-29; NuGet's policy needs a use by 2026-11-03. Any push to `main` touching `packages/`
  publishes too.

## Next

- Ask the developer first: close v0.1, and which slice opens v0.2 — T11 N3, the pick Console (T10 § 6), else Shares
  (T11 § 3). Never open v0.2 before both answers.
- Open points: T11 N2 (park the drive-shaped backlog rows); T10 U1, U2, U4–U8 (U5, sign-in before Identity, gates the
  console's first iteration).
- Buildable without Identity or the migration module: T11 § 2. Blocked: console sign-in by token, org gates, per-tenant
  issuers (Identity, contracts in T8); the Ocharo import and the shared-host deploy (T9, D6).

## Cloud notes

- Toolchain: .NET SDK `10.0.300` (`global.json`), Node `>=24.11 <25`, pnpm `10.33.2`; unverified in this environment.
- `dotnet test engineering/codebase/files.backend-services/Files.BackendServices.slnx` runs every tier and builds the
  web app into `wwwroot` first (needs pnpm, no Docker). Frontend: `pnpm -C engineering/codebase/files.frontend-services`
  with `test`, `lint`, `format:check`, `typecheck`, `check:package`.
- Absent here: the hub's git guard, the local preview servers (`:8393` API, `:8394` web) and the browser. E2E tests are
  the check; say what stayed unverified in a browser.
- Git, as the guard enforced locally: subject-only conventional messages, `{type}: {past-tense verb} {what}`, 50–70
  characters, no body; check `git var GIT_AUTHOR_IDENT` names the developer before committing; no force push, no tags
  (CI creates `v*`, `be-v*`, `fe-v*`), no committed package versions.
- A missing or wrong be sdk API is fixed in `wow-two-sdk-beta/wow-two-sdk-beta.be`, released by its CI, then re-pinned
  here — never worked around in Files. That repo is outside this checkout; hand the change to the developer if it
  cannot be cloned and pushed from here.
- Build traps: `engineering/development/backend-guidelines.md` § Traps.
- The developer reads compact bullets, one decision per question.
