# Party foundation progress

- Plan: `plans/party-foundation/PARTY_FOUNDATION_PLAN.md`
- Roadmap: `plans/party-foundation/PARTY_FOUNDATION_ROADMAP.md`
- Roadmap item: `party-foundation/core`
- Worktree: `C:\Users\TommySeery\source\repos\Concertable\b2b\.worktrees\Refactor-PartyFoundationLegacyBindings`
- Branch: `Refactor/PartyFoundationLegacyBindings`
- PR: [#18](https://github.com/Concertable/b2b/pull/18)
- Reviewed base: `309e40d4b4b704fe94246332130566b89f464de4`
- Current checkpoint: P1 4.7 Conversation identity, audience, sequencing and safe delivery complete
- Dependency/package gates: none for the current local P1 slice
- Delivery gate: do not push or merge; the user authorized local P1 implementation and commits only.
- Last reconciled: 19 September 2026 against the current worktree and local verification artifacts

## Current state

P1 slices 1-4, 4.3, 4.1 and 4.7 are implemented. Conversations now have explicit identity, immutable
participant tenants, durable create/send request receipts, exact Read/SendMessages grants, locked sequence
allocation, membership-scoped monotonic read positions and Tenant-owned versioned display projections.
Routes, clients, notifications, tests, migrations and guidance use Conversation throughout; delivery sends
invalidation only and reauthorizes current readers.

The Tenant initial migration retains its hand-authored `tenant.MembershipAuthority` view after regeneration.
Conversation read-position writes use a dedicated repository and an atomic range-locked maximum update.
Shared, Artist and Venue clients consume the conversation API and close/reopen notifications across tenant
switches.

## Next Steps

Scope: current slice only; full plan remains incomplete.
Current slice: P1 4.8 neutral business lifecycle and real client consumption.
Remaining scope: P1 4.8-4.10, then P2-P5 and terminal delivery remain open.
Done when: the 4.8 backend and web/mobile cutover is built, its focused gates pass, and the slice is committed locally.

Replace business-profile eligibility rows with Tenant business activities; complete atomic neutral Tenant
creation/settings/deletion, version naming and invitation/owner safety. Deliver the authenticated Business web
and mobile journeys, compose every eligible surface, correct verification contracts, and make tenant switching
a generation-fenced session boundary for queries, subscriptions and commands.

## Completed work

- Slice 1, commit `5ab4356b`: membership snapshots, audiences, module-local exact grants, principal issuance,
  Concert summary sharing/member assignment and deletion of generic Application/Booking sharing.
- Slice 2, commit `b2001b2f`: deleted ambient host privilege; explicit privileged processing, published
  Concert projection and outcome receipt correctness.
- Slice 3, commit `f6ecc9bf`: regenerated access migrations, authority view, grant keys/indexes and seeding
  stances.
- Slice 4: independent ordinary connections plus one enlisted root command transaction, quiescent
  event/outbox flushing, authority validation and settlement/invoice atomicity.
- Slice 4.3: Application, Booking and Concert mutations use exact command policy, resource/grant locks,
  durable share replay and provider-real race coverage; `ApplicationSide` is deleted.
- Slice 4.1, this commit: typed permission identity end to end, strict ASP.NET name parsing, typed catalogs and
  client serialization at the wire boundary, with value/policy/catalog tests.
- Slice 4.7, this commit: explicit Conversation identity/audience, idempotent create/send, exact grants,
  sequenced messages, monotonic read positions, Tenant displays, invalidation-only delivery and complete
  backend/frontend/schema/guidance cutover.

## Verification

- `dotnet build Concertable.B2B.slnx --no-restore -m:4`: passed, 0 errors; 3 pre-existing E2E nullable warnings.
- Conversations unit tier: 38 passed; Tenant unit tier: 141 passed.
- Conversations integration tier: 17 passed; final read-position repository candidate reran its focused case.
- Architecture tier: 24 passed.
- Tenant and Conversations canonical InitialCreate drift checks: clean.
- Shared frontend: 18 tests passed and package build passed; Artist and Venue production builds passed.

Still required before a P1 completion claim: every module integration tier, provider race/revocation ordering,
workers, contract/invoice reads, external-summary denial, real browser/native evidence and canonical review.
The complete Concert and Lifecycle tiers retain their recorded 4.10 rerun obligations. The Startup resource-
graph case must be rechecked where Stripe CLI is available.

## Reviews

No review yet for the completed P1 candidate; the canonical review remains the final 4.10 gate.

## Decisions, discoveries, blockers, and deviations

- Tenant remains the business/legal/membership/settlement identity; tokens remain identity-only.
- Eligibility, membership permission and resource audience remain separate facts.
- No ambient host bypass, query-filter bypass, generic private-details endpoint or unchecked load/save.
- P1 external disclosure is Concert Summary only; member assignments stay inside a principal tenant.
- Protected commands use one local transaction; ordinary and parallel reads use independent connections.
- External payment/blob calls remain outside the database transaction and reuse durable operation identity.
- The product is pre-launch: no compatibility layer, backfill or retained old vocabulary.
- Migrations stay owned by the filtered context; privileged contexts perform explicit system work.
- The architecture gate initially found a stale direct `Reunion.Validation` package reference in Concert unit
  tests; no source consumed it, so it was deleted and the gate reran green.
- Windows could not load SqlClient SNI from the deep worktree path; integration tests pass through a temporary
  short `R:` mapping to the same checkout.

## External/deferred owners

P2-P5, dependency publication, configurable-workflow documentation, configurable RBAC, tenant retirement,
sales evidence, payment quote disclosure and transport qualification retain their existing plan owners and
gates. No sibling checkout is modified by this P1 run.
