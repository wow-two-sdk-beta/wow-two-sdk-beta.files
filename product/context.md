# wow-two-sdk-beta.files — Product Context

*Last updated: 2026-10-26*

> Current state — changes often. Durable definition: `product.md`.

## Current state

The repo exists on GitHub since 2026-10-26, private until its first release, scaffolded from the domain template. The
spec and the nine-track analysis moved in from the hub, every point confirmed by the developer. The contract skeleton,
the Formats and Server projects and a 43-row file-type registry seed sit beside the template's Greetings sample; v0.1
(File store) is planned and not started. Nothing is published.

## Active tasks

| Task | Status | Notes |
|---|---|---|
| v0.1 File store | todo | `engineering/planning/version-track/v0.1/v0.1.md` |
| Make the repo public, then add the `main-guard` ruleset | todo | developer; the free plan refuses rulesets on a private repo |
| Archive `wow-two-platform/wow-two-platform.storage.file` | todo | developer; [legacy notes](../engineering/research/legacy-storage-file.md) |
| Identity contracts C1–C15 | in progress | prompt ready; the developer forwards it; integration as we go |
| Wheelhouse migration module prompt | draft | refined once the import API exists, then forwarded |
| NuGet and npm first publish | todo | after v0.1 replaces the sample (`create-library` phase 4) |

## Open questions

- D6: the byte store, and the shared host's server.
- D8: when Desk's `bytea` files move; recommended last.

## Decisions

- 2026-10-21 — build File Storage in three forms; Ocharo first.
- 2026-10-25 — one instance per owner; a file lives while any record references it.
- 2026-10-26 — its own domain repo: one files domain covering storage, server, formats, compression, images,
  documents, video and icons; name `files`, org `wow-two-sdk-beta`.
- 2026-10-26 — one image with three roles; a shared files host beside owner instances; WoW2 and Ocharo start on it.
- 2026-10-26 — D7: files go out publicly through share links behind gates.
- 2026-10-26 — Identity: offline token validation, client credentials, token exchange in P2; contracts C1–C15, kept as
  contracts until the final integration.
- 2026-10-26 — the first test migration runs through a Wheelhouse migration module (W1–W9).
- 2026-10-26 — moved core namespaces become `Files.Images` and `Files.Documents`.
- 2026-10-26 — planning follows the version track: one backlog with the whole vector, one version doc at a time.
