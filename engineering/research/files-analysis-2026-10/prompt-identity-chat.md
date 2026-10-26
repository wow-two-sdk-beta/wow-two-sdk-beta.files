# Prompt — Identity chat: contracts Files needs

*Last updated: 2026-10-26*

> Ready to forward to the Identity chat as one message. Everything below the line is the prompt.

---

Files — our new files domain (storage, sharing, processing, compression, its own repo; name being settled) — is an
API resource of our Identity. Its analysis is done and the developer confirmed it on 2026-10-26. Files needs 15
contracts from Identity, the kernel or both; settle them in the identity analysis so the library and server ship them.

**Read first**

- `wow-two-ws/workbench/wow-two-sdk-beta/wow-two-sdk-beta.files/engineering/research/files-analysis-2026-10/t8-identity-contracts.md` — the contracts C1–C15 with shapes.
- `wow-two-ws/workbench/wow-two-sdk-beta/wow-two-sdk-beta.files/engineering/research/files-analysis-2026-10/t5-identity.md` — why Files needs each one.
- `wow-two-ws/workbench/wow-two-sdk-beta/wow-two-sdk-beta.files/engineering/research/files-analysis-2026-10/t1-services-tenancy.md` § 2 — instance kinds: a per-owner instance
  (A) and a shared files host (B) that trusts several Identity instances at once.

**Facts that shape the asks**

- Ocharo and the WoW2 apps start on B, so B trusts WoW2's and Ocharo's Identity instances side by side.
- Files P1 does not wait for Identity: it starts on tenant API keys and the existing client-credentials handler, then
  switches each tenant to its Identity issuer once that instance is live.
- browsers never hold Files tokens (your Q7, BFF only): they upload and download through tickets and signed URLs their
  BFF mints.
- Files reads one active organization (your Q8), validates offline by JWKS with no introspection (Q9), and mirrors
  "the product checks the acting user" (Q11) with delegation headers under a `files.delegate` scope (C5).
- references stay one way: Files → `Identity.Abstractions` and `Identity.Validation`; Identity never references Files
  (avatars go through the C13 port).

**New to the identity analysis**

- C1 — a kernel principal seam beyond `ICurrentUserService` (`Guid? Id`, `UserKind`): string subject, client, issuer,
  organization, roles, permissions, scopes, actor, partition. A core change, released before Identity and Files re-pin.
- C8 — multi-issuer JWT validation: `ITrustedIssuerResolver` picks issuer, metadata and audience per token; today's
  `JwtOptions` takes one issuer.
- C9 — API keys in the shared claim vocabulary: `ApiKeyRecord` gains client, organization, partition and expiry.
- C10 — the OIDC client module picks its scheme at runtime per tenant (P2), for tenant admins and share sign-in on B.
- C13 — an `IAvatarStore` port in `Identity.Abstractions`, with a default over core `Storage/`.
- C15 — subjects are unique per issuer only; Identity keeps `sub` stable and never reused.

**Already on your list, now with Files' shapes**

- C2 claim vocabulary · C3 resources and scopes, audience = the Files instance URL · C4 client credentials ·
  C6 token exchange (P2) · C7 JWKS and key rotation (I17, I18) · C11 guest email proof (Q21) · C12 events and webhooks
  (P2) · C14 tenant onboarding steps.

**When Files needs them**

- Files P1: C1, C2, C3, C4, C7, C8, C9, C14, C15.
- Files P2: C6, C10 for tenants, C11, C12. Identity P2: C13.

**What to do**

1. For each of C1–C15: accept, change or reject; write the pick into the identity analysis decision queue and, once
   the developer confirms, into the identity spec.
2. Place each accepted contract in an identity slice (I14–I22 or new ones) with its phase.
3. Leave Files' side alone: C5, the tenant registry and the resolver and repository implementations are Files'.
4. Reply with one line per contract (C-number → accepted / changed / rejected, slice), and list every shape change
  Files must mirror.
