# wow-two-sdk-beta.files — Product Context

*Last updated: 2026-11-08*

> Current state — changes often. Durable definition: `product.md`.

## Current state

The repo is public on GitHub since 2026-10-27, with the `main-guard` ruleset, scaffolded from the domain template. v0.1
(File store) is built and in verification: file types and icons, tenant-scoped deduplicated storage, references,
signed and public delivery, the HTTP API with key and token callers, the audit trail; the template's sample is gone.
The image releases from `main`. CI published the packages first on 2026-10-27: NuGet `WoW2.Sdk.Beta.Be.Files.*`
`10.0.0-beta` and npm `@wow-two-beta/fe-files` `0.0.2`, both through trusted publishing.

## Active tasks

| Task | Status | Notes |
|---|---|---|
| v0.1 File store | verification | `engineering/planning/version-track/v0.1/v0.1.md`; the developer runs the checks |
| Console slice | proposed | [T10](../engineering/research/files-analysis-2026-10/t10-console.md) § 6; its points wait for the developer |
| Shares as containers | proposed | [T11](../engineering/research/files-analysis-2026-10/t11-next-and-shares.md) § 3; N1–N3 wait for the developer |
| Identity integration | in progress | v0.1 iteration 8; picks in [T8](../engineering/research/files-analysis-2026-10/t8-identity-contracts.md) and `engineering/planning/files-integration.md` |
| Wheelhouse migration module prompt | draft | refined once the import API exists, then forwarded |

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
- 2026-11-08 — Identity: token exchange moves to P1 and replaces the acting headers; a person's call needs the scope and
  a `files:` permission; a shared host's audience is each tenant's own host; P1 trusts only Ocharo's Identity
  (`files-integration` #12, #13, #23, #29, #35, #36).
- 2026-10-26 — the first test migration runs through a Wheelhouse migration module (W1–W9).
- 2026-10-26 — moved core namespaces become `Files.Images` and `Files.Documents`.
- 2026-10-26 — planning follows the version track: one backlog with the whole vector, one version doc at a time.
- 2026-10-27 — Files serves files to products, service to service; a person-facing drive is a separate server built
  later on the same packages; expiry and shares stay in Files ([T11](../engineering/research/files-analysis-2026-10/t11-next-and-shares.md)).
- 2026-10-27 — console stats keep a daily rollup from the first console build (T10 U3).
- 2026-10-27 — a "container" is a share: the owner edits its file set, the link shows the current set; no
  collection entity (T11 N1).
- 2026-10-27 — the service serves every file icon from its web root at `/icons/{ext}.svg`, embeddable by any origin.
