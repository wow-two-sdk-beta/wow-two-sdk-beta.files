# wow-two-sdk-beta.files — Product Context

*Last updated: 2026-10-27*

> Current state — changes often. Durable definition: `product.md`.

## Current state

The repo is public on GitHub since 2026-10-27, with the `main-guard` ruleset, scaffolded from the domain template. v0.1
(File store) is built and in verification: file types and icons, tenant-scoped deduplicated storage, references,
signed and public delivery, the HTTP API with key and token callers, the audit trail; the template's sample is gone.
The image releases from `main`; no package is published yet.

## Active tasks

| Task | Status | Notes |
|---|---|---|
| v0.1 File store | verification | `engineering/planning/version-track/v0.1/v0.1.md`; the developer runs the checks |
| Console slice | proposed | [T10](../engineering/research/files-analysis-2026-10/t10-console.md) § 6; its points wait for the developer |
| Identity contracts C1–C15 | in progress | prompt ready; the developer forwards it; integration as we go |
| Wheelhouse migration module prompt | draft | refined once the import API exists, then forwarded |
| NuGet and npm first publish | todo | `create-library` phase 4: NuGet consent, npm first version (developer) |

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
