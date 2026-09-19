# Party foundation progress

- Plan: `plans/party-foundation/PARTY_FOUNDATION_PLAN.md`
- Roadmap: `plans/party-foundation/PARTY_FOUNDATION_ROADMAP.md`
- Roadmap item: `party-foundation/core`
- Worktree: `C:\Users\TommySeery\source\repos\Concertable\b2b\.worktrees\Refactor-PartyFoundationLegacyBindings`
- Branch: `Refactor/PartyFoundationLegacyBindings`
- PR: [#18](https://github.com/Concertable/b2b/pull/18)
- Reviewed base: `309e40d4b4b704fe94246332130566b89f464de4`
- Current checkpoint: P1 4.1 DDD permission value complete and locally verified
- Dependency/package gates: none for the current local P1 slice
- Delivery gate: do not push or merge; the user authorized local P1 implementation and commits only.
- Last reconciled: 19 September 2026 against the current worktree and local verification artifacts

## Current state

P1 slices 1-4, the complete shipped mutation surface in 4.3, exact-scope reads and the 4.1 authorization
DDD polish are implemented. Application, Booking and Concert reads expose operation-specific contracts;
protected mutations use the root command transaction, locked current authority, exact grants, privileged
repositories and final post-flush validation. Provider race and rollback coverage remains recorded in the
prior P1 commits.

`TenantPermission` is now a closed `readonly record struct` with canonical instances, values, parsing,
equality and formatting. Catalogs, membership/resource contexts, policies, requirements and runtime callers
use the value type. Only membership DTO serialization and ASP.NET policy/attribute names use strings;
attributes parse and reject undeclared names immediately. The architecture gate also removed the unused
Concert unit-test `Reunion.Validation` reference it exposed.

## Next Steps

Scope: current slice only; full plan remains incomplete.
Current slice: P1 4.7 Conversation identity, audience, sequencing and safe delivery.
Remaining scope: P1 4.8-4.10, then P2-P5 and terminal delivery remain open.
Done when: the 4.7 replacement is built, its focused gates pass, and the slice is committed locally.

Replace `Thread` with `Conversation` across code, schema, routes, clients, tests and guidance. Address create
and send operations by `ConversationId`; add durable create/send receipts, immutable initial audience,
explicit Read/SendMessages grants, locked message sequence allocation, monotonic membership read positions,
Tenant-owned display projections and invalidation-only delivery that reauthorizes current readers. Delete
participant-set lookup/deduplication, counterpart inference, message-content activities and every old name.
End with the solution build, focused unit/integration coverage, architecture gate and a local commit.

After 4.7, complete neutral Tenant lifecycle/client journeys in 4.8/4.9, then the P1 qualification matrix and
canonical review in 4.10.

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

## Verification

- `dotnet build Concertable.B2B.slnx --no-restore`: passed, 0 errors; only pre-existing warnings.
- Authorization unit tier: 47 passed.
- Tenant unit tier: 141 passed.
- Architecture tier: 24 passed after deleting the unused Concert unit-test package reference.
- Previous provider baseline remains valid: Application integration 75 passed; Booking integration 24 passed;
  focused Concert exact-scope/cancellation/settlement and Lifecycle exact-scope regressions passed.

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

## External/deferred owners

P2-P5, dependency publication, configurable-workflow documentation, configurable RBAC, tenant retirement,
sales evidence, payment quote disclosure and transport qualification retain their existing plan owners and
gates. No sibling checkout is modified by this P1 run.
