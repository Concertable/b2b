# B2B commercial execution progress

- Plan: `docs/plans/commercial/COMMERCIAL_EXECUTION_PLAN.md`
- Roadmap: `docs/plans/commercial/COMMERCIAL_ROADMAP.md`
- Roadmap item: `commercial/execution`
- **Recovery: `RECOVERY.md` beside this file — read it first. This design line reached no polyrepo until
  2026-09-14 and its relationship to `DEAL_CONFIGURATION_PLAN.md` is an open question, not a settled one.**
- Branch: none. Recovered from the archived monorepo branch `Docs/launch_booking-entry-direct-offers`
  (16 commits, tip `4d82045bc`), which was never merged and had no PR.
- Dependency/package gates: planning-only; provider/model and remaining executable contracts require qualification
- Last reconciled: 2026-09-09; Tommy accepted the independent PostgreSQL recommendation, selected AgreementSnapshot and requested complete incorporation in the existing plan; owner HEAD b7af7d697499f8ef49779cc8089e8b9c01231fab, clean paths/worktrees and absence of an owning PR verified before editing

Artifact paths are relative to the B2B source root, which is `api/` in this repository (it was
`api/Concertable.B2B/` in the monorepo these documents were written in).

## Current state

D29's architecture is approved: relational composition and named typed references; bounded TPH for
Term, DealTerm and StepConfiguration; immutable JSONB snapshots for proposed/accepted commercial
content and prepared execution. No provider-dependent interim is needed. D30 retains Deal's stored
configuration ownership and each module's lifecycle, mandatory guards and executable implementation.

The immutable DTO is AgreementSnapshot; ProposalSnapshot carries proposal-specific hash/rendering facts.
Section 3 owns entity members; section 7 now preserves the complete independent recommendation:
reasons/disadvantages, provider limitations/evidence, entity diagram, table/EF/DTO/request shapes,
ownership and every identity/version, constraints, queries/cache/growth, worked lifecycle/financial
cases, source-based migration and qualification gates. Section 16 consumes those snapshot boundaries.

The accepted editing model has one editable Deal candidate and immutable proposal content, including
private unissued proposals. A candidate edit creates a new ProposalId/hash; it does not change an old
snapshot, inherit its consent/readiness or withdraw the current issued offer. Draft template retargeting
uses a non-key FK and atomic value replacement. Accepted ContractRevision is the agreement itself.

This checkpoint updates planning artifacts only. No runtime implementation, provider migration,
package change, publication or new market research is authorized. Concrete EF/Npgsql compilation,
constraint/race tests and complete payment/collection/settlement/cancellation contracts remain gates.
The approval does not resolve D9-D12, D28 or enable B6/B7/customer-builder functionality.

D19-D27 remain intact: Dunet PaymentMethodStep; typed family enum plus Version defaulting to exactly 1;
shared generic strategy/union infrastructure; operation Request naming; Checkout/CheckoutAsync;
inherited GetByIdAsync with a shape specification; four-preset parity and distinct entry paths.

## Next Steps

Continue the original workstream's design qualification using section 7's approval boundary and section
16's executable-contract walkthrough. The PostgreSQL architecture choice is resolved; do not commission
the same open-ended persistence review again or substitute a TPC/JSONB interim without new evidence.

1. Complete the four-preset payment/collection/settlement/cancellation input/result and checkpoint
   descriptors against the named runtime owner, including Preparation errors and accepted commitments.
2. Resolve remaining D28 replay/library/binding and entry authority/Result-terminal excerpts; do not
   promote their older raw Guid-header, commandJournal or null-forgiving sketches into approved code.
3. Retain D9-D12, exact retained-data/cutover and D22 support-policy gates with their existing owners.
   Re-resolve source/package ownership before an implementation slice.
4. Obtain explicit implementation scope before application code, generated migrations or tests are
   changed. The saved provider/constraint/financial qualification gates must pass in that slice.

## Completed work

- Earlier B2B-owned plan consolidated PR633/later lifecycle evidence, docs PR11, organizer scenarios,
  authority/resource boundaries and coordinated delivery slices.
- f9c9264940b92715b9d52d9077df9d738702bc11: D19-D22 typed union/version/factory and support directions.
- 8dd7b1c2c, ebf3989b0 and 654d242fd: four-preset/query naming, required Template/actual Terms,
  reusable workflow ownership and D30's deliberate configuration-contract boundary.
- b7af7d697: concrete term/step relationships consolidated and independent PostgreSQL review commissioned.
- This commit: independent architecture accepted and fully reconciled in section 7, D29, dependent
  sketches/slices and this existing ledger; AgreementSnapshot naming and immutable proposal revisions
  replace incompatible earlier envelopes. No application source changed.

## Verification

- Source evidence at independent review: monorepo main a7323c3617e65d539f92160fa17623cdb3ef4835;
  extracted B2B main fded052cdbf6f0f4c8f55ef7414c13ffc19ab33c remained older than PR633.
  B2B source at planning b7af7d697 matches PR633; later hosting/package/confirmed-booking serialization
  changes do not alter the inspected Deal mapping, financial formulas or keyed builders.
- Provider evidence was checked on 9 September against current first-party EF/Npgsql/PostgreSQL docs;
  section 7 contains the supporting links. No EF compilation, migration or performance result is claimed.
- This checkpoint's scoped verification: root/B2B plan graphs, local links/anchors, code fences,
  decision continuity, snapshot naming/immutability consistency, recommendation coverage and
  git diff --check. Verification is documentation-only.
- Earlier broad reachability inspection reported baseline Dashboard CLAUDE sibling, E2EAdmin and
  KeyedStrategies test guidance issues; they remain with their owners before publication.
- Product evidence remains docs main 99ad353b/PR11 and research 5ac4048; unpublished research is not
  approved policy. No new market research or fresh product-policy qualification occurred.

## Reviews

The independent source-based persistence review concluded the permanent target and Tommy accepted it.
Section 7 is its durable result. This checkpoint receives scoped documentation consistency self-review;
no compiled model, generated migration, benchmark or implementation-readiness proof is implied.
Remaining executable/API excerpts require their own concrete review before implementation.

## Decisions, discoveries, blockers, and deviations

- Section 7 owns D29's reasons, alternatives, disadvantages and evidence; do not retain another review
  ledger or treat JSONB complex-type inheritance as a blocker for the approved relational graph.
- Shared declarations mean the exact same negotiated input; published dependencies and memberships
  are sealed. Type-witness FKs and final-state membership checks are required, not merely typed C# properties.
- HashSet/IReadOnlySet with reference equality remains the EF relationship choice; ImmutableArray of
  immutable values is the snapshot/DTO choice. Canonical hashing does not depend on set iteration.
- Preserve additive Versus, current payer/recipient direction, revenue basis and rounding, frozen
  settlement amount and stable operation identity. Existing PaymentMethod metadata is not Save/Verify/Authorise.
- Application and Invitation stay distinct; the separate entry acceptance record remains. Booking's
  ContractRevision is the accepted agreement, and Concert receives its own immutable snapshot.
- Naming allowlist: SelfBillingAgreementDocument and the historical BookingAgreementDocument label
  describe real PDF/document rendering, not the renamed DTO; their existing source/history stays intact.
- Shared generic builders use explicit supported behavior/version pairs with preserved coverage,
  overlap, duplicates and lifetime checks. A union catalogue does not isolate identical interface/key DI collisions.
- Existing retained data is unverified: preserve it until D10's exact cutover is qualified. InitialCreate
  regeneration never authorizes deletion or replay of completed external payments.
- Payment owns provider financial facts; external ticket revenue is evidence, not available funding.
- Shared DataAccess/Messaging, commission, tenant configuration, lifecycle seals and operation recovery
  keep their existing owners. PostgreSQL adoption precedes B5; this does not reopen all-service migration.
- Stale generic specification guidance remains an upstream dotagents/agent-standards issue; do not edit
  installed caches or unrelated owner worktrees from this plan.
- The unrelated main checkout and other active worktrees are unchanged. The service-local index records
  this architecture approval; root launch ordering and sibling dependency edges have not changed.
