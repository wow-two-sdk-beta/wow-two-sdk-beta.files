# wow-two-sdk-beta.files — Engineering Rules

*Last updated: 2026-10-26*

> Working rules for agents in this repo — repo-specific deltas only. Shared rules: wow-two-ws/conventions/.

- read the active version doc and the backlog before planning work; the analysis in `engineering/research/` is
  evidence, not the plan.
- add a package project only with its first code; keep `package-registry.md` in step.
- keep Identity behind its contracts (T8) until the final integration; never reference Identity's implementation.
- route a needed core change to the be sdk first, then move the pin here.
