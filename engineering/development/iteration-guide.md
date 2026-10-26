# wow-two-sdk-beta.files — Iteration Guide

*Last updated: 2026-10-26*

How a slice moves: plan → implement → verify → consolidate → close. Repo-specific deltas only.

- every broker change lands in Module, Client and the conformance suite in the same iteration.
- a new file type is a row in `packages/Formats/Registry/file-types.json`, never a code change.
