# Code review — Refactor/PartyFoundationLegacyBindings

> **This file is a work order, not a discussion.** If you're handed this file, fix the open `[ ]`
> findings directly and report what changed. Tick each `[x]` as you land it. Pause only for a genuinely
> irreversible or ambiguous finding: record its durable disposition, take the safe path, and keep going.

**Review status:** `in-progress`
**Reviewed up to commit:** `8eacaf0130a88e265d604f224fdf462d80eeb936`  `(2026-09-20)`
**Security-reviewed up to commit:** `8eacaf0130a88e265d604f224fdf462d80eeb936`  `(2026-09-20)`
**Judgment:** `changes-requested`

**Branch restart — 2026-09-15:** the branch was reset to origin/main and the rejected runtime commits
dropped, so the first pass below reviews code that no longer exists on any branch. Its F1–F10 are
void as work items; they are retained only for the reasoning that fed the replan, and F10's
Participant decision already lives in the ledger. The commit identities recorded in both passes are
reflog-only. The second pass's approval of the plan documents still stands — their content survived
the restart unchanged apart from reconciling it to the new starting state.

## Review pass — 2026-09-15 — full (void: candidate discarded by branch restart)

**Candidate base:** `2b5264b4c9748931c840c816e99408d537bec7c2`
**Candidate head:** `38eb9032e9ba882e358de80f15962bb0a8b3e6da`
**Candidate branch:** `Refactor/PartyFoundationLegacyBindings`
**Candidate scope:** `all`
**Candidate path-set:** `sha256:7309dec08f9703ddeaedc5965708fbfbdd4fcca8ded919e6db15cca1cbde2a47` `(21 paths)`
**Candidate bundle:** `C:\Users\TommySeery\source\repos\Concertable\b2b\.git\agent-workflow\runs\review-partyfoundation-p1-20260915124945\review\dbd4e2818d1ec1dc6a65c620733e0e2470ca4a3285e4231a2c87fe2958386910`
**Candidate bundle identity:** `sha256:246e4d7d69a7d0166377e424c6f4c56ad4b4973726980bec3e972dc9f704cb08`
**Work-order path:** `reviews/Refactor-PartyFoundationLegacyBindings.md`
**Work-order mode:** `new`
**Pass judgment:** `changes-requested`

Layers run: native/general (correctness, equivalence, error handling, test quality) and a repository-conventions
lens over the routed standards. No path qualified the security inventory, so no security marker is stamped.

**Primary risk cleared.** Both lenses independently confirmed no direction flip on any of the five bindings.
The decisive evidence is `ConcertEntityConfiguration.cs:31`: `DealType` *is* the TPH discriminator, so moving
dispatch from "which CLR leaf" to "what the stored `DealType` says" reads the same column and cannot diverge.
`ConcertChangedEvent` keeps all 19 arguments in order and value; supplier/customer still key the invoice
sequence, VAT calculation and self-billing gate, preserving the VenueHire reversal.

### Findings

- [x] **F1 — HIGH — conventions/native** — `api/src/Modules/Concert/AGENTS.md:24`, `api/src/Modules/Deal/ARCHITECTURE.md:237`, `api/src/Modules/Deal/LEGAL_REQUIREMENTS.md:85`
  Three guidance docs reachable from the `@`-loaded root `AGENTS.md` still rostered `DealPayeeResolver`,
  `IDealPayeeResolver`, `VenuePaysArtistDealPayeeResolver` and `ArtistPaysVenueDealPayeeResolver` as current
  code, in the present tense. All four are deleted by this candidate. Because these load every prompt, the
  staleness actively misdirected the next agent on settlement direction, and `LEGAL_REQUIREMENTS.md` pointed
  the outstanding VAT work at a deleted type.
  **Fixed:** `Deal/ARCHITECTURE.md` section 2.8 rewritten to describe the five bindings and the surviving
  ticket-user family; `Concert/AGENTS.md` money-flow pointer repointed; `LEGAL_REQUIREMENTS.md` repointed.

- [x] **F2 — MEDIUM — conventions** — `plans/party-foundation/PARTY_FOUNDATION_PROGRESS.md:89`
  The ledger claimed a repository-wide search returned nothing outside build output. That search was run with
  `--include=*.cs --include=*.ts --include=*.tsx` and could not see Markdown; three doc hits existed in the
  same commit (F1). This is the exact failure the global rule "never claim absence off an indirect search"
  names: the method could not see the target, and the negative was recorded durably anyway.
  **Fixed:** claim restated as scoped to C#/TS sources, with the miss and its repair recorded.

- [x] **F3 — LOW — native** — `api/src/Modules/Concert/Concertable.B2B.Concert.Domain/ValueObjects/LegacyFinancialParties.cs:37`
  Direction exhaustiveness dropped from compile-time to runtime. On the base, `SettlementPayerTenantId` and
  `SettlementPayeeTenantId` were `abstract`, so a new `ConcertEntity` leaf could not compile without declaring
  its direction; at head an unhandled `DealType` reaches `throw` inside `SettlementService.ReserveAsync` and
  `InvoiceIssuer.IssueAsync`. `KeyedStrategyBuilder.RequireAll` still fails composition for the keyed families
  (verified: it delegates to `RequireExactly(Enum.GetValues<TKey>())`), so startup is guarded until those arms
  are added — but the direction table itself is no longer coverage-checked, and four explicit `[InlineData]`
  rows stay green when a fifth member is born.
  **Fixed:** added `Resolve_EveryDealType_IsBound`, asserting over `Enum.GetValues<DealType>()`.

- [x] **F4 — LOW — conventions** — `api/src/Modules/Concert/Concertable.B2B.Concert.Domain/ValueObjects/LegacyFinancialParties.cs:7`
  `Resolve` canonicalizes the value (rejecting `Guid.Empty` and unknown presets) but the positional record left
  a **public** constructor, so any assembly inside the `InternalsVisibleTo` set could build a
  direction-inconsistent instance, and positional `init` accessors left `with` as a second bypass.
  `STYLE.md`: "Keep construction private when ordinary creation canonicalizes the value".
  **Fixed:** explicit get-only properties with a private constructor. `Resolve` is now the only entry point and
  `with` can no longer break the five-value consistency.

- [x] **F5 — LOW — conventions/native** — `api/src/Modules/Concert/Concertable.B2B.Concert.Domain/ValueObjects/LegacyFinancialParties.cs:3-6`
  The XML summary restated five already-named properties, then narrated the decision this commit made
  ("Nothing else in Concert may re-derive a direction from `DealType`") — design narration the global
  zero-comment rule names as its worst offender. It was also factually wrong: the retained `ITicketUserResolver`
  family *does* key on `DealType`, so the comment forbade something the same commit kept doing.
  **Fixed:** deleted; the invariant lives in the commit message.

- [x] **F6 — LOW — native** — `.../UnitTests/Domain/ConcertEntityTests.cs:63`, `.../UnitTests/Resolvers/TicketUserResolverTests.cs:45`
  The `DealType`-to-`ConfirmedBookings` switch was duplicated character-for-character across two test files,
  beside the shared factory that should own it.
  **Fixed:** `ConfirmedBookings.For(DealType)` added; both call sites use it.

- [x] **F7 — LOW — conventions** — `.../UnitTests/Domain/ConcertEntityTests.cs:27`
  `SettlementTenants_Preset_ComeFromTheLegacyFinancialBinding` ended on the internal collaborator rather than
  the observable outcome (`UNIT.md`: "the last segment names the observable outcome").
  **Fixed:** renamed to `SettlementTenants_Preset_FollowTheDealTypeSupplyDirection`.

- [x] **F8 — LOW — conventions** — `.../UnitTests/Resolvers/TicketUserResolverTests.cs:40`
  The test asserted `concert.FinancialParties.TicketBeneficiaryTenantId` — a value its Act never produces and
  its name does not claim — duplicating coverage `LegacyFinancialPartiesTests` already owns.
  **Fixed:** assertion and its now-unused local removed.

- [x] **F9 — LOW — conventions** — `.../UnitTests/Domain/ConcertEntityTests.cs:44,55`
  Both new gross tests merged Act into the assertion and carried a guard `Assert.False(...)` inside Arrange,
  losing the three-phase separation `UNIT.md` requires.
  **Fixed:** guard dropped (a failed declaration already surfaces in the gross assertion) and the Act lifted.

- [wontfix] **F10 — MEDIUM — conventions** — `api/src/Modules/Concert/Concertable.B2B.Concert.Domain/ValueObjects/LegacyFinancialParties.cs:3`, `api/src/Modules/Concert/Concertable.B2B.Concert.Domain/Entities/ConcertEntity.cs:213`
  **Open — needs Tommy's decision; do not resolve silently.**
  `Concert/AGENTS.md:31-33` reserves `Party` for the invoice snapshot VO and states: do not use the bare word
  "party" as generic glue for "a venue/artist tenant" elsewhere. `LegacyFinancialParties` and
  `ConcertEntity.FinancialParties` are exactly that glue, and the reserved holder (`InvoiceParty`) is real and
  unchanged in the same folder. Separately, `NAMING.md` — "a qualifier only exists to contrast with a sibling"
  — is violated by `Legacy`: a whole-tree scan finds no non-legacy counterpart, because the native binding type
  is not born until P4.
  Two coherent resolutions, diverging on vocabulary this plan reuses through P2-P6:
  **(a)** rename to the ROLE words `Concert/AGENTS.md` already defines — `SettlementDirection` carrying
  `PayerTenantId`, `PayeeTenantId`, `SupplierTenantId`, `CustomerTenantId`, `TicketPayeeTenantId` — which also
  clears the `Legacy` qualifier and makes the code agree with that doc's existing
  "supplier = settlement payee, customer = ticket payee" mapping;
  **(b)** keep the plan's `Party` vocabulary (P2 introduces `AgreementParty`, `RevisionParty`, `PartyBinding`
  and `PartySlot`, so `Party` becomes core) and deliberately rewrite the reservation sentence in
  `Concert/AGENTS.md` in the same change.
  Left open rather than decided, because (a) discards the plan's "beneficial recipient" slot name, which exists
  precisely so P4 can let the collector and the beneficiary differ, while (b) bends a module rule to fit
  unreviewed code. Blocking merge until settled.

  **Disposition — 2026-09-15:** Tommy rejected the entire abstraction and authorised a replacement plan.
  The rewritten plan sections 2–3 reserve InvoiceParty, select Participant, and require P1 to delete the
  rejected value and its consumers/tests/guidance references. That owning plan is the removal work order;
  no rename of the rejected implementation is useful. This planning request does not execute removal or
  approve the old runtime candidate for delivery. Resolution condition: P1's whole-source removal gate.

### Checked and clean

- **Behavioural equivalence, all five bindings plus ticket user** — verified against the deleted overrides, the
  two deleted keyed arms, and the old `InvoiceIssuer` and `SettlementService` reads.
- **`ResolveSettlementTenantId` removal** — had exactly one consumer (the deleted facade) and one test
  assertion; no production behaviour dropped.
- **`SettlementPreparation.Ready`** — still receives payer then payee in that order; `PayoutCompleteStep`
  destination unchanged.
- **Keyed-strategy rule** — the single switch is sanctioned, not a violation. `CODE_PATTERNS.md`: "where the
  variation is data rather than injected behaviour, `DealType` selects a type, not a strategy". And
  `KEYED_STRATEGIES.md` scopes its anti-pattern to an *otherwise agnostic* component, with "the rule lives in
  exactly one resolver". The change reduces the places knowing direction from eight to one.
- **EF mapping** — no `Ignore`; all three properties are setter-less and unmapped before and after, and appear
  in no `IQueryable` projection. The no-migration claim holds and was confirmed by `has-pending-model-changes`.
- **Visibility and layering, DI shape, test tier, assertion library, expected-value-last `[InlineData]`, no
  `CreateSut()` factory** — all compliant.
- **Tests are non-tautological** — expectations come from literals in the attribute, not from re-deriving the
  production switch; the Versus case asserts 200 where greater-of would yield 100.

### Post-remediation verification

`dotnet build Concertable.B2B.slnx` succeeded, 0 errors. All 14 unit and architecture projects: **533 passed,
0 failed** (Concert 119, the extra test being F3). Integration and browser tiers remain exact-head CI gates;
the local Docker daemon is not running.

## Review pass — 2026-09-15 — docs replan

**Candidate base:** `70d1c9e655d1c5ffd5a10b367153616b861af2b4`
**Candidate head:** `ab2cf866b469dcf65925061007e1598438622207`
**Candidate branch:** `Refactor/PartyFoundationLegacyBindings`
**Candidate scope:** `plans/party-foundation (three changed Markdown files only)`
**Candidate path-set:** `sha256:a11878bfbbd0319cb71aa5f675e121fd7c8b2df5c3b187587e2234817f44c424` `(3 paths)`
**Candidate patch:** `sha256:3ef7090a3c81088f3c6de92537ae640ac4121b5f4c24b86d756199d2a9358da9`
**Candidate bundle:** `C:/Users/TommySeery/source/repos/Concertable/b2b/.git/agent-workflow/runs/party-foundation-replan-review-20260915/review/6007892ecfbb0110602d97767ea167617cf3b75bb79e001b03d052f6b2f87d95`
**Candidate bundle identity:** `sha256:5b8c57582b584ac1e592f59e00cdc8a9685be35288015a140d824a7b1dfe7cc1`
**Work-order path:** `reviews/Refactor-PartyFoundationLegacyBindings.md`
**Work-order mode:** `append`
**Pass judgment:** `changes-requested`

This pass reviews the requested planning replacement, not the rejected runtime changes preceding its base.
It cannot establish that the runtime branch is merge-ready. No remote synchronization was requested for
this immutable local planning candidate. The installed helper selected native-general; absent repo-local
routing/reachability tooling is already owned by TECH_DEBT.md. Apply the supplied root instructions,
plan-authoring/plans/plan-checkpoint and documentation accuracy/followability rules manually. No guidance
file or executable path changed, so no runtime security watermark is claimed.

### Findings

Fresh native-general and contract/followability agents reviewed the same frozen descriptor
`c0cf6970c3184b21d2c6585f480701cfe99f05e7a3598163195914f6e459390c` independently. Both returned the
recorded head/path/bundle identities. The parent checked each retained candidate against the frozen
plan and corresponding source, deduplicated the consent finding, and added phase/schema checks.
No source edits or runtime tests were performed by the reviewers.

- [x] **F11 — MEDIUM — native** — `plans/party-foundation/PARTY_FOUNDATION_PLAN.md:235`
  P1 replaces Membership.type but names nonexistent app/web/b2b and omits app/shared and app/mobile.
  The actual shared hook derives permissions locally; RootNavigator selects venue/artist from type.
  Update all real consumers, define neutral/multi-profile mobile navigation, and qualify the existing
  web/mobile build scripts plus tenant-switch and restricted-member journeys.
  **Fixed:** P1 names app/shared, all actual web roots and mobile; defines neutral/profile navigation,
  server permission consumption, tenant-switch isolation and existing web/mobile build/native gates.

- [x] **F12 — HIGH — native/contracts** — `plans/party-foundation/PARTY_FOUNDATION_PLAN.md:359`
  Arbitrary additional principals need consent, but the only later signature operation always returns
  a Booking-backed receipt. Define intermediate consent collection with no Booking and an idempotent
  final acceptance path; prove three principals can sign one revision across separate requests.
  **Fixed:** RecordConsent returns a consent receipt without Booking; Accept optionally adds the last
  consent and atomically validates the complete set. Proposal replacement returns content for review;
  issue/submit signs the returned hash. Three-principal/replay/revocation checks are phase gates.

- [x] **F13 — HIGH — native/contracts** — `plans/party-foundation/PARTY_FOUNDATION_PLAN.md:446`
  The blanket prohibition on child insertion under a sealed proposal prevents later Consent rows.
  Separate immutable proposed content from append-only consent evidence; seal the copied accepted
  consent set. Test direct-SQL content protection alongside permitted later consent insertion.
  **Fixed:** proposal content seals before signatures; entry consent is separate append-only evidence;
  acceptance copies and seals its qualifying consent set. Distinct EF/SQL mutation gates are explicit.

- [x] **F14 — HIGH — contracts** — `plans/party-foundation/PARTY_FOUNDATION_PLAN.md:277`
  Application preparation receives OpportunityId, but neither Opportunity's proposed schema nor its
  authoring contract supplies the authoritative Show/Slot. Define that immutable mapping, withdrawal/
  revision checks and the common slot used by competing applications and direct invitations.
  **Fixed:** Opportunity owns a non-retargetable Show/Slot reference and policy version; authoring and
  Application preparation inherit validated slot/schedule/location authority. Withdrawal/slot edits
  share the acceptance fence. DirectInvitation selects the same slot contract.

- [x] **F15 — HIGH — contracts** — `plans/party-foundation/PARTY_FOUNDATION_PLAN.md:316`
  AgreementSnapshot/ConfirmedBooking omits selected performer/location identities while Concert
  creation requires ArtistId/VenueId and the performer fence needs ArtistId. Pin the typed attachments,
  tenant ownership and scheduling facts in the proposal and carry them unchanged downstream.
  **Fixed:** PerformanceAttachment pins typed Artist/Venue identities, owner tenants, schedule, venue-use
  authorization and agreed facts. ConfirmedBooking carries it unchanged; allowed public-copy refreshes
  cannot alter accepted identity, period, resource or economics.

- [x] **F16 — HIGH — parent/followability** — `plans/party-foundation/PARTY_FOUNDATION_PLAN.md:267`
  P1 promises membership revocation fences, but authoritative version fields and the shared transaction
  coordinator first appear under P2. Move the required Tenant versions, transaction enlistment and
  existing protected-write integration into P1; P2 must extend the same coordinator to acceptance.
  **Fixed:** P1 now owns authority version storage, the policy relation and shared-connection transaction
  integration for current writes. P2 extends it. Real-provider rollback/revocation gates are explicit.

- [x] **F17 — MEDIUM — parent/contracts** — `plans/party-foundation/PARTY_FOUNDATION_PLAN.md:367`
  Durable request receipts and commitment/financial intents have behavioural prose but no owning
  persistence contract, key/state/receipt lifetime or event-correlation shape. Specify the minimal
  module-local records and their uniqueness, state transitions, FKs and opaque operation routing.
  **Fixed:** defined aggregate-owned RequestReceipt, FinancialIntent, PaymentCommitment and PaymentOutcome
  records, uniqueness/lifetime/state transitions, exact provider routing and reconciliation; P1/P2
  ownership is explicit. Also made evidence deadline/overdue behaviour and inventory-only publication
  claims precise. Planning graph, local links/headings, state-provider resolution and diff checks pass.

- [x] **F18 — MEDIUM — parent/source-accuracy** — `plans/party-foundation/PARTY_FOUNDATION_PLAN.md:215`
  Profile-neutral onboarding/eligibility lacks a business-contact source: Tenant stores no ContactEmail
  and Announce sends LegalName, while the proposed promoter cannot borrow a profile's contact identity.
  Define Tenant-owned legal/contact/eligibility facts and zero-profile businesses. Also name the moved
  role/permission contracts so Tenant implementing Authorization's port cannot create a dependency cycle.
  **Fixed:** Tenant stores ContactEmail and serves BusinessFacts; onboarding allows zero profiles and
  verification contacts use Tenant facts. Authorization owns moved request/role/permission contracts;
  Tenant implements its port. Planning graph and diff whitespace checks pass for this group.

### Parent judgment

Changes requested on the planning candidate. Its source survey, no-live-data replacement stance,
Participant vocabulary, payer/payee scope, sibling claim reuse, consumer publication chain and 32-item
debt dispositions are supported. F11–F18 are reversible plan corrections within the original request.
After remediation, require a fresh incremental documentation review. This pass does not validate
the rejected runtime commits outside its base/head range.

## Review pass — 2026-09-15 — incremental documentation

**Candidate base:** `ab2cf866b469dcf65925061007e1598438622207`
**Candidate head:** `62fd01e1f5ca49c033389d9f1ce73559aa68b086`
**Candidate branch:** `Refactor/PartyFoundationLegacyBindings`
**Candidate scope:** `planning remediation and canonical review bookkeeping only`
**Candidate path-set:** `sha256:1ea5adcd09b679cadf47f72d02cfa5826a0acdaf27fe9e68b392568336955474` `(3 paths)`
**Candidate patch:** `sha256:05de3fcc0a8033e589d89394824289f445545538ad65d7782744a8b23c1056cf`
**Candidate descriptor:** `12f6a8c07fd59aae82d822ac3d495ab9e8228fc548501f283d9b72f0a53a025f`
**Candidate bundle:** `C:/Users/TommySeery/source/repos/Concertable/b2b/.git/agent-workflow/runs/party-foundation-replan-incremental-20260915/review/cb0280501ce5f8eacb247e851c1cec1986a2fdc768bce6b4986479531183c489`
**Candidate bundle identity:** `sha256:779cc053d3bb4798b357bbe8ec95b0cc561069d2e85967eb5846ed7a486c1649`
**Work-order path:** `reviews/Refactor-PartyFoundationLegacyBindings.md`
**Work-order mode:** `append`
**Pass judgment:** `approved`

Fresh native-general and contract/followability lenses receive only the plan/progress region and frozen
source context. The parent alone validates work-order bookkeeping so reviewers do not see prior findings.
The helper-selected paths also contain this review artifact; that path remains in the recorded identity.
No runtime code changed, no security-path watermark applies, and no remote synchronization is claimed.

### Findings

No new findings. Both fresh lenses returned completed results with the exact recorded base, head,
descriptor, path digest and bundle identity. They checked authority/transaction ownership, shared/web/
mobile consumers, Tenant facts, intermediate consent and separate content seals, slot/performance
attachments, durable financial records and independent zero-ticket publication. Both independently
confirmed the plan requires replacement without adapters, fallback readers, backfills or parallel events.

The parent validated the evidence and work-order delta. Original F1–F10 text, candidate identities and
completed pass judgments are unchanged except permitted statuses/dispositions. F11/F16/F18 were fixed
in `0fddbfea45474df608feba2925599ca577835742`; F12–F15/F17 in
`62fd01e1f5ca49c033389d9f1ce73559aa68b086`. Every finding has a terminal disposition.

Planning graph, local links/headings, repository-state resolution and diff checks pass. The plan,
ledger and roadmap are committed; only this final review watermark is local work-order state. No
runtime implementation, new runtime validation, publication or deployment occurred. Approval covers
the rewritten documentation and its remediation; rejected runtime work still requires P1 replacement
and the implementation phases' own review/delivery gates.

## Review pass — 2026-09-18 — full (P1 repair slices 1–3)

**Candidate base:** `3272bbad0843005a1a915c2b60bce02f1a1266cd`
**Candidate head:** `735eae64af97d8431ed93b2270234ac39bfb4b94`
**Candidate branch:** `Refactor/PartyFoundationLegacyBindings`
**Candidate scope:** `all`
**Candidate path-set:** `sha256:90626a16b378865aff490c78db1aa4c1761577448d774152616232278050ba7f` `(221 paths)`
**Candidate bundle:** `C:/Users/TommySeery/source/repos/Concertable/b2b/.git/worktrees/Refactor-PartyFoundationLegacyBindings/agent-workflow/runs/review-p1-foundation-20260918/review`
**Candidate bundle identity:** `sha256:df3c68de5dfeceea3bce9ad81d6c495cfafbc51a9515b2e080618a27c100e61c`
**Work-order path:** `reviews/Refactor-PartyFoundationLegacyBindings.md`
**Work-order mode:** `append`
**Pass judgment:** `changes-requested`

First review of runtime implementation on this branch. The candidate is the three P1 repair slices
(`5ab4356b`, `b2001b2f`, `f6ecc9bf`) plus their two ledger commits. 195 of the 221 paths are
hand-written; 26 are regenerated migrations and Reqnroll-generated feature code.

Layers run: native/general (correctness, reuse, efficiency, error handling); an access-predicate lens; a
persistence/DI-composition lens; a domain-events/seeding lens; a changed-behaviour test-impact lens; and the
security layer — 17 changed paths matched the authorization inventory, so the security marker is owed.

Three lenses independently reached finding R1 from different starting points. Where the security lens and the
persistence lens disagreed on privileged-stance reachability, the parent re-traced the call graph and the
persistence lens is correct: the security lens traced `POST /api/concert/{id}/cancel` and the bus processors
but missed `DevController` (R2).

### Findings

- [ ] **R1 — critical — every Concert ACL command decides against the caller-visible grant subset, not the ACL.**
  `ConcertService.cs:320, 369, 388, 416, 441` all load through `IConcertRepository.GetWithGrantsByIdAsync`
  (`ConcertRepository.cs:31-34`), whose `Include(c => c.AccessGrants)` runs on the filtered `ConcertDbContext`.
  The grant filter (`ConcertDbContext.cs:39-51` via `ResourceAccessExpressions.cs:21-31`) admits only
  `grant.TenantId == ActiveTenantId` and `MembershipId == null || == ActiveMembershipId`, so a grant issued
  *to another tenant* or *to another member* is structurally absent. Consequences, all reachable:
  `AlreadyShared` (`ConcertEntity.cs:122-128`) can never fire, so a duplicate share falls through to a
  unique-index `DbUpdateException` that `TrySaveChangesAsync` does not absorb; `RevokeExpiredSummaryShares`
  (`:151-157`) retires nothing, breaking the expiry-then-reissue the comment at `:146-147` describes;
  `RevokeSummaryShare` (`:167`) returns `GrantNotFound` for the issuer's own share, making disclosure
  irrevocable through the API; `ReplaySummaryShareAsync` (`ConcertService.cs:369-376`) cannot find its grant;
  `AssignMember`/`RemoveMemberAssignment` (`:187-190`, `:216-220`) cannot see another member's assignment.
  Plan §4.5 names this exact trap. The correct mechanism was written and never wired:
  `IConcertPrivilegedRepository.GetWithGrantsByIdAsync` and `GetIdentityByIdForUpdateAsync`
  (`IConcertPrivilegedRepository.cs:15,19`) have no caller.
  **Fix:** inject `IConcertPrivilegedRepository` into `ConcertService` and route those five paths through it,
  using `GetIdentityByIdForUpdateAsync` for the pre-load authority check; the domain already fences the write
  (`IsPrincipal`, `IssuedByTenantId`). The receipt repository and unit of work must move to the same context.

- [x] **R2 — critical — an authenticated caller can trigger settlement on any concert.**
  `DevController.Complete` (`DevController.cs:19-27`) is `[Authorize]`-only, takes `concertId` from the query
  string, and has no environment gate — the host's only environment conditionals are Swagger and seeding
  (`B2BWebHostExtensions.cs:281,287`). It calls `IConcertWorkflow.CompleteAsync`
  (`ConcertWorkflow.cs:49-70`) → `SettlementService.ReserveAsync` → `IUnitOfWorkBoundary`, which this
  candidate rebound from `ConcertDbContext` to `ConcertPrivilegedDbContext`
  (`FactoryUnitOfWork.cs:7-11`). At base the grant filter bounded that endpoint to concerts the caller could
  see; it no longer does. The `[Authorize]`-only trigger predates this candidate, the removal of its
  containment does not.
  **Fix:** register the endpoint only when `!IsProduction`, or delete it. Do not rely on the doc comment.
  **Disposition:** deleted `DevController`; the Concert API builds cleanly and the production route no longer exists.

- [x] **R3 — high — `RemoveMemberAssignment` reports success when it revokes nothing.**
  `ConcertEntity.cs:210-227`: zero matches is indistinguishable from a revocation, the controller returns 204
  (`ConcertController.cs:61-67`), and `AccessVersion++` sits outside the loop so it bumps on a no-op —
  invalidating every other caller's expected version for a change that did not happen. Contrast
  `RevokeExpiredSummaryShares` (`:160`), which bumps correctly inside the loop. It is also the only one of the
  four ACL commands with no `ExpectedAccessVersion` (`ConcertService.cs:433-453`). With R1 this fires on every
  call: an operator removing a member's access is told it worked while the grants stay live.
  **Fix:** add an explicit `NotAssigned` arm, bump `AccessVersion` only when a row was revoked, and take and
  check `ExpectedAccessVersion` as the other three commands do.
  **Disposition:** member removal now requires and checks `expectedVersion`, returns the typed
  `concert.member_assignment.not_assigned` 404 when no live assignment exists, and increments `AccessVersion`
  only after revoking the matching grants. The access-control integration test proves stale removal leaves
  grants/version unchanged, valid removal revokes both scopes and bumps once, and a repeated removal returns
  404 without another bump. The focused graph builds warning-free and all five access API tests pass.

## Review pass — 2026-09-20 — incremental

**Candidate base:** `3ffe856c77a59c960218e9816f6b591a04cd7712`
**Candidate head:** `553314378a76c6a24e472c0c0f8db811d0d9ac7e`
**Candidate branch:** `Refactor/PartyFoundationLegacyBindings`
**Candidate scope:** `all`
**Candidate path-set:** `sha256:7601fb545f818cf44a83de4098da77f823a1d81142409862b580be564c89bdd9` `(9 paths)`
**Candidate bundle:** `C:\Users\TommySeery\source\repos\Concertable\b2b\.git\agent-workflow\runs\party-foundation-remediation-20260920\review\incremental-553314378a76c6a24e472c0c0f8db811d0d9ac7e`
**Candidate bundle identity:** `sha256:baff1dc48da9190e3ed4f5da6ff4501ac802b42a6b5956674449d33c35a6b44c`
**Work-order path:** `reviews/Refactor-PartyFoundationLegacyBindings.md`
**Work-order mode:** `append`
**Pass judgment:** `approved`

### Findings

Both native/general and security/concurrency lenses found no actionable issue. R3 is closed across domain,
application, HTTP, authorization, concurrency, and regression coverage.

- [x] **R4 — high — `Concertable.B2B.Authorization.UnitTests` is not in the solution and does not compile.**
  The project is absent from `Concertable.B2B.slnx` (only `.Contracts` and `.Infrastructure` are listed) and
  `git log -S` shows it was never listed. CI builds and tests through that solution
  (`.github/workflows/ci.yml:59,62,73`), so nothing has ever compiled or run it. It holds the only coverage of
  `MembershipContext` resolution and `PermissionCatalog` — the authority source this work rewrites, including
  `IsHost_IsNeverTrue` and the header-selection cases. It carries five references to symbols this candidate
  deletes: `PermissionCatalogTests.cs:66-68` and `MembershipContextTests.cs:181-182`
  (`TenantRole.RestrictedParticipant`), and `MembershipContextTests.cs:100` (`MembershipFact`).
  `MembershipContextTests.cs:107-121` also still asserts that a malformed header falls back to the sole
  membership, which `MembershipContext.cs:71-72` now rejects.
  **Fix:** add the project to `Concertable.B2B.slnx`, delete the five stale rows, and rewrite the
  malformed-header test to assert the throw. This invalidates the earlier "0 failed across 14 assemblies"
  claim, which silently excluded this assembly.
  **Disposition:** the stale role/fact rows and malformed-header expectation were already corrected in the
  current source; the missing Authorization test project is now in the solution. Its previously excluded
  assets file still resolved Kernel alpha.0.5 and reproduced the `IsHost` `TypeLoadException`; an explicit
  current-graph restore moved it to alpha.0.14. All 46 Authorization unit tests now pass, and the full solution
  builds with zero warnings and errors while including the project.

## Review pass — 2026-09-20 — incremental

**Candidate base:** `553314378a76c6a24e472c0c0f8db811d0d9ac7e`
**Candidate head:** `bd13e9bb5e93b5c5a68ec9698bba08b0e31ea336`
**Candidate branch:** `Refactor/PartyFoundationLegacyBindings`
**Candidate scope:** `all`
**Candidate path-set:** `sha256:4607e44fc1a193e12801385647ef622a1543ecd638d9b4df8c3b6ac503db144a` `(2 paths)`
**Candidate bundle:** `C:\Users\TommySeery\source\repos\Concertable\b2b\.git\agent-workflow\runs\party-foundation-remediation-20260920\review\incremental-bd13e9bb5e93b5c5a68ec9698bba08b0e31ea336`
**Candidate bundle identity:** `sha256:fdd37bb8b1bb67222a5a78425427e108aeaea0483d077936275c0fe8d8a03d1a`
**Work-order path:** `reviews/Refactor-PartyFoundationLegacyBindings.md`
**Work-order mode:** `append`
**Pass judgment:** `approved`

### Findings

Both native/general and security/test-inclusion lenses found no actionable issue. R4 is closed with the
Authorization unit tier now present in the solution and current-graph evidence.

- [x] **R5 — high — F10 is not fixed: the published projection has no caller.**
  `IConcertReadRepository.GetPublishedByIdAsync` (`ConcertReadRepository.cs:29-41`) is referenced only by its
  own interface and implementation. `ConcertController.GetDetailsById` (`ConcertController.cs:29-34`) still
  calls `GetDetailsByIdAsync`, which carries no `DatePosted` predicate. An unpublished draft remains reachable
  by direct id. The slice-2 commit message and the ledger both claim F10 closed.
  **Fix:** route the public read to the published projection, and correct the ledger.
  **Disposition:** the current controller already routes `GET /api/concert/{id}` to `GetPublishedAsync`, which
  calls `GetPublishedByIdAsync` and filters on `DatePosted != null`. The focused integration regression passes:
  a posted concert returns 200 and an unpublished draft returns 404. The stale finding is reconciled without a
  further code change.

## Review pass — 2026-09-20 — incremental

**Candidate base:** `bd13e9bb5e93b5c5a68ec9698bba08b0e31ea336`
**Candidate head:** `c5150e7cd7f2bda0fe371e9b1925fd631942268b`
**Candidate branch:** `Refactor/PartyFoundationLegacyBindings`
**Candidate scope:** `all`
**Candidate path-set:** `sha256:0f8265121c3f273d86be4af1d16b5791ff1dbf1659f57f488b35f2023bf0d460` `(1 path)`
**Candidate bundle:** `C:\Users\TommySeery\source\repos\Concertable\b2b\.git\agent-workflow\runs\party-foundation-remediation-20260920\review\incremental-c5150e7cd7f2bda0fe371e9b1925fd631942268b`
**Candidate bundle identity:** `sha256:3f10d41712f88084942f2813ba979b8a4af7c801943fcb66e30a63bdd0048e65`
**Work-order path:** `reviews/Refactor-PartyFoundationLegacyBindings.md`
**Work-order mode:** `append`
**Pass judgment:** `approved`

### Findings

Both native/general and security lenses approved the frozen one-path ledger delta with no actionable
findings. R5 is closed on the exact published-only call chain and focused posted/unpublished regression.

- [x] **R6 — medium — the mid-command flush throws where every sibling returns a typed error, and commits a
  revocation before the command's guards run.** `ConcertService.cs:333` is the one bare `SaveChangesAsync` among
  the command paths; it carries the concert rowversion mutated by `RevokeExpiredSummaryShares`, so a lost race
  escapes as `DbUpdateConcurrencyException` (500) instead of `Superseded`. It also commits the revocation
  before `ShareSummary` (`:335`) has checked `IsPrincipal` and `validUntil` (`ConcertEntity.cs:116-120`), so a
  `NotPermitted`/`InvalidValidity` rejection leaves a committed revocation behind.
  **Fix:** use `TrySaveChangesAsync(... => e is DbUpdateConcurrencyException, ct)` → `Superseded`, and run the
  pure guards before the revocation.
  **Disposition:** added a pure domain validation step before expiry revocation and moved the intermediate
  flush through the privileged unit of work's tolerant save, mapping a lost concurrency race to `Superseded`.
  The focused invalid-validity regression proves the existing expired share and access version remain
  unchanged; the full access class passes 6/6 and Concert unit tests pass 90/90.

## Review pass — 2026-09-20 — incremental

**Candidate base:** `c5150e7cd7f2bda0fe371e9b1925fd631942268b`
**Candidate head:** `8eacaf0130a88e265d604f224fdf462d80eeb936`
**Candidate branch:** `Refactor/PartyFoundationLegacyBindings`
**Candidate scope:** `all`
**Candidate path-set:** `sha256:e430ba48b3561cc1b4bc619d122306bbfcf02b1b820a89d8a1cb8861a59e50dd` `(8 paths)`
**Candidate bundle:** `C:\Users\TommySeery\source\repos\Concertable\b2b\.git\agent-workflow\runs\party-foundation-remediation-20260920\review\incremental-8eacaf0130a88e265d604f224fdf462d80eeb936`
**Candidate bundle identity:** `sha256:3794d2a539ce913b366b24471495c7a3b1c970e7cc2adb381b68bcee699d1f72`
**Work-order path:** `reviews/Refactor-PartyFoundationLegacyBindings.md`
**Work-order mode:** `append`
**Pass judgment:** `approved`

### Findings

Both native/general and security/concurrency lenses approved the frozen eight-path R6 delta. They verified
that the row lock and one command transaction span both saves, and that typed failure rolls back the flush.

- [x] **R7 — medium — a duplicate-key violation escapes as a 500 on the very path the receipt exists to make
  idempotent.** `ConcertService.cs:360` tolerates only `DbUpdateConcurrencyException`. Two concurrent copies of
  the same `RequestId` both pass the receipt check at `:306` and the loser gets a duplicate-key
  `DbUpdateException`. An `IsDuplicateKey()` helper already exists and is used for exactly this at
  `AdminService.cs:149,160` and `WriteRepositoryExtensions.cs:21`.
  **Fix:** widen the predicate, and on a duplicate receipt re-read it and return the replay; a duplicate grant
  maps to `AlreadyShared`.
  **Disposition:** duplicate-key persistence failures are now recovered only after the failed command
  transaction rolls back. A fresh command scope revalidates authority, locks and reloads the concert and
  receipt, replays a matching receipt, returns `RequestConflict` for a mismatched replay, and maps the remaining
  duplicate-grant case to `AlreadyShared`. Focused replay/concurrency tests pass 2/2; the access class passes 6/6
  and Concert unit tests pass 90/90.

- [ ] **R8 — medium — `ResourceCommandReceipt.HashPayload` is not injective and is not `DateTimeKind`- or
  culture-stable.** `ResourceCommandReceipt.cs:42-54`: the `U+001F` separator is neither escaped nor
  length-prefixed, so `("aU+001Fb","c")` and `("a","bU+001Fc")` collide; the `null` sentinel `"U+0000"`
  collides with a literal `"U+0000"`; `part.ToString()` uses the current culture; and `DateTime.ToString("O")`
  encodes `Kind`, so the same instant sent as `…Z` and `…+01:00` hashes differently and a genuine retry gets
  `RequestConflict`. The last case is reachable today through `request.ValidUntil`.
  **Fix:** length-prefix each part, normalise `DateTime` with `ToUniversalTime()`, and format every
  `IFormattable` with `CultureInfo.InvariantCulture`.

- [ ] **R9 — medium — replay maps a server-side inconsistency to `ConcertNotFound` for a concert it just
  loaded, permanently.** `ConcertService.cs:369-376` returns `ConcertNotFound` (404) after the concert loaded
  successfully, when the receipt's recorded grant cannot be resolved. Because the receipt is durable, every
  later retry of that `RequestId` takes the same branch.
  **Fix:** treat an unresolvable recorded outcome as an invariant violation (throw, as
  `SettlementPaymentProcessor.cs:51-59` does) or give it its own error arm. Decide separately what a replay
  should report when the grant was since revoked.

- [ ] **R10 — medium — cross-tenant existence probes run before the caller's authority over the concert is
  established.** `ConcertService.cs:313-318` calls `tenantModule.GetByIdAsync` and `IsCurrentMembershipAsync`
  on caller-supplied GUIDs before loading the concert at `:320`. Any holder of `resources.share` gets a
  distinguishable `InvalidRecipient` for a concert id they hold no grant on. Bounded by v4 GUID space, so an
  ordering defect rather than a usable oracle.
  **Fix:** load the concert and run its `NotFound`/`Superseded` checks first.

- [ ] **R11 — medium — `MalformedTenantHeaderException` is mapped nowhere.**
  `MembershipContext.cs:71-72` throws it from `TenantResolutionMiddleware`, which runs for the whole pipeline
  (`B2BWebHostExtensions.cs:256`). The only two references in the tree are the throw and the declaration — no
  handler, no `ProblemDetails` arm — so it conventionally surfaces as 500 for what is a client error. It also
  fires for *duplicate* `X-Tenant-Id` headers, because `TryGetHeaderTenantId` parses `values.ToString()`
  (`MembershipContext.cs:84-86`), which comma-joins. Not reachable pre-authentication:
  `MembershipContext.cs:51-55` returns for an anonymous caller before any header parsing.
  **Fix:** map it to 400 at the Web host, or short-circuit in the middleware.

- [ ] **R12 — medium — `IsCurrentMembershipAsync` materialises every membership of a tenant to answer one
  boolean, on a request path.** `TenantService.cs:57-61` calls `ListMembershipsByTenantAsync` (tracked entities,
  `MembershipRepository.cs:43-44`) then filters in memory. It runs before every share and every member
  assignment (`ConcertService.cs:317,413`). The same repository already shows the right shape at `:52-53`.
  **Fix:** add `ExistsByTenantIdAndIdAsync` as an `AnyAsync` and call it.

- [ ] **R13 — medium — `IInvoiceSequenceRepository.InsertAsync` has `AddAsync` semantics.**
  `InvoiceSequenceRepository.cs:19-20` stages without saving, while the persistence standard fixes
  `InsertAsync` as stage-and-save and every inherited implementation in the codebase saves.
  **Fix:** rename to `AddAsync` on interface and implementation.

- [ ] **R14 — medium — two registered services have no consumer and the code they replace is unchanged.**
  `IInvoicePrivilegedRepository` and `IInvoiceSequenceRepository` are registered
  (`ServiceCollectionExtensions.cs:84-85`) with no injection site, while `InvoiceIssuer` still does all three
  jobs against the context directly (`InvoiceIssuer.cs:26,50-51,55,70`). Plan §4.6 requires `InvoiceIssuer` to
  take those repositories and drop its `DbContext` parameter; this candidate created them and stopped.
  **Fix:** wire `InvoiceIssuer` to them (this is 4.5/4.6 work and may be deferred to that slice, but the
  registrations should not sit dead in the meantime).

- [ ] **R15 — medium — the `IsHost` bypass shape survives on two live predicates.**
  `TenantFilters.cs:24` still ORs `context.TenantContext.IsHost` into every single-owner filter, and
  `ConcertService.cs:273` still ANDs `!tenantContext.IsHost`. Both are inert only because both implementations
  hard-code `false` (`MembershipContext.cs:36`, `DesignTimeTenantContext.cs:12`). `ITenantContext` is a
  platform package type, so the member cannot be removed here — but the disjuncts can, and two unit tests
  already mock it `true` (`ConcertServiceTests.cs:117,157`), asserting through a bypass production cannot
  reach.
  **Fix:** drop both local disjuncts and the two `true` mocks.

- [ ] **R16 — low — `AssignMember`'s tenant guard is vacuous.** `ConcertEntity.cs:181-185` compares
  `membershipTenantId != actorTenantId`, and the sole caller passes `actor.TenantId` for both
  (`ConcertService.cs:423`). The real check is `IsCurrentMembershipAsync` at `:413`; the domain guard looks
  like a second barrier and is not.
  **Fix:** drop the parameter and the comparison, or pass the membership's actual owning tenant.

- [ ] **R17 — low — the shared audience predicate is copy-pasted ten times across four contexts.**
  `ConcertDbContext.cs:43-46,48-51,57-60`; `BookingDbContext.cs:29-32,38-41`;
  `ApplicationDbContext.cs:31-34,36-39`; `ConversationsDbContext.cs:36-39,41-44`. Plan §4.2 requires the
  shared membership/audience/time expression to be expressed once in DataAccess's expression builder;
  `ResourceAccessExpressions` factored out the membership/validity half and stopped, leaving the
  security-critical `MembershipId == null || == ActiveMembershipId` rule with ten edit sites.
  **Fix:** add an `Or` combinator and a `ReachableAtAudience<TGrant, TScope>(context, permission)` expression;
  each context then supplies only its scope test.

- [ ] **R18 — low — dangling doc references to the deleted `AccessScopedDbContext`.**
  `TenantScopedDbContext.cs:13,53`, `TenantFilters.cs:10` and `CODE_PATTERNS.md:14,20` still name the type this
  candidate deletes. The two source files are outside the changed-path set, so the candidate broke references
  in unchanged files — the rename's grep gate was not run.
  **Fix:** rename all five to `ResourceScopedDbContext` and run `grep -rniE "accessscopeddbcontext|accesscontext"`
  to zero.

- [ ] **R19 — low — stray U+FEFF mid-file.** `ConcertServiceTests.cs:3` and `ConcertServiceCreateTests.cs:3`
  (byte offset 87 in both) carry a BOM in the middle of the file, from prepending `using` lines above a
  BOM-bearing first line. Compiles, but is junk.
  **Fix:** strip both.

- [ ] **R20 — critical — posting a concert returns 500. Proven by test, not predicted.**
  Slice 3 rebound three Concert pre-commit domain-event handlers to `IConcertPrivilegedRepository`, so they
  now re-read the aggregate through a *different* `DbContext` instance than the one mid-save.
  `ConcertEntity.Post` (`ConcertEntity.cs:289-302`) sets `DatePosted` in memory and raises the event;
  `DomainEventDispatchInterceptor` dispatches pre-commit handlers *before* the UPDATE reaches SQL; so
  `ConcertPostedDomainEventHandler.cs:25,31` materialises the pre-write row where `DatePosted` is NULL and
  `!.Value` throws. `TrySaveChangesAsync` catches only `DbUpdateException`, so it escapes as a 500 and the
  concert never leaves `Draft`.
  **Verified:** `dotnet test api/tests/Concertable.B2B.Lifecycle.IntegrationTests --filter ConcertPostingLifecycleTests`
  → 2 failed, `PUT /api/concert/post/1` → `500 "Nullable object must have a value."` at
  `ConcertPostedDomainEventHandler.cs:31`.
  `ConcertChangedDomainEventHandler.cs:34-51` has the same defect *silently*: on the update path it publishes
  the pre-update `Name`, `About`, `TotalTickets` and `Genres` to every downstream projection, so an edit never
  reaches the listing and no test fails. `ConcertCancelledDomainEventHandler` is safe only by accident — it
  reads three immutable fields.
  Reverting is not the fix: the filtered binding returned nothing in membership-less flows, which is why it
  was changed.
  **Fix:** stop re-reading in a pre-commit handler. Either resolve the read from the saving context via
  `IDbContextAccessor.Context` (both interceptors set it for the duration of dispatch), or carry the
  projection on the domain event — `ConcertChangedDomainEvent` already does this for `Price`, `Period` and
  `DatePosted` — and delete the re-read. The second also removes the latent explicit-transaction hazard.

- [ ] **R21 — high — unsettled — adding a filter to the grant *entity* may have emptied three Conversations
  participant queries.** Before this candidate `ThreadAccessGrant` had no filter of its own; the conditions were
  inlined into each parent's `Any(...)` (patch 7254-7269). The candidate moves them onto the grant entity
  (`ConversationsDbContext.cs:32-44`), which restricts grants to `grant.TenantId == ActiveTenantId`. Three
  queries deliberately read *other* participants' grants and were changed only for the `Participate` →
  `SendMessages` rename: `ThreadRepository.cs:21-28` (`Distinct().Count() == participantTenantIds.Count` can
  now only ever be 1, so `MessageService.cs:85-96` would create a duplicate thread on every send),
  `ThreadRepository.cs:35-43` (`RecipientsOfAsync` yields an empty recipient list),
  `MessageRepository.cs:74-81` (asks `grant.TenantId != tenantId` while the filter asserts `==`, so provably
  empty).
  **Status: not settled.** The Conversations integration suite cannot execute in this worktree —
  all 17 tests fail at fixture startup in 1ms with
  `DllNotFoundException: Microsoft.Data.SqlClient.SNI.dll ... The filename or extension is too long (0x800700CE)`,
  a Windows MAX_PATH limit on this deep worktree path. That is an environment failure and is evidence of
  nothing about the code.
  **Fix:** run this suite from a shorter path (or with long paths enabled) to settle it first. If confirmed,
  serve participant/counterparty lookups from `ConversationsPrivilegedDbContext`, or keep the grant conditions
  inlined in the parent filters as before. Plan §4.7 deletes `GetByParticipantsAsync` and `CounterpartTenantId`
  anyway, but the candidate as frozen ships these call sites live.

- [ ] **R22 — medium — the permission catalog grants Staff a permission it can never exercise.**
  Every principal grant is issued tenant-wide (`membershipId: null` at `ConcertEntity.cs:98`,
  `ApplicationEntity.cs:65`, `InvoiceEntity.cs:81`, `ThreadEntity.cs:43`), and the `AssignedResources` arm
  requires `grant.MembershipId == ActiveMembershipId`. The only member-grant issuer is
  `ConcertEntity.AssignMember` (Summary + Operations). So Staff/Door/Sound read nothing anywhere except a
  concert explicitly assigned to them — yet `PermissionCatalog.cs:78-83` gives Staff `MessagesRead` and
  `MessagesSend`, which no conversation grant can ever satisfy.
  **Fix:** either issue member-assignment grants on the conversation alongside the concert assignment, or
  remove the message permissions from the assigned-audience roles until P1 has a path that makes them usable.
  Whichever way, the catalog should not assert a capability the predicate forbids.

- [ ] **R23 — low — the settlement succeeded/failed processors now disagree on the same condition.**
  `SettlementPaymentProcessor.cs:48-53` throws for an outcome naming an unknown concert;
  `SettlementPaymentFailedProcessor.cs:38-43` logs and records the inbox receipt for the identical condition.
  Same event family, opposite poison policy: one dead-letters and needs an operator, the other consumes
  silently. The throw does reach durable retry (Service Bus abandons with backoff and dead-letters at
  `MaxDeliveryCount`; the outbox dispatcher records a bounded failure), and no receipt is written on that path
  — both verified.
  **Fix:** pick one policy for an unresolvable target across both processors, and distinguish "concert does not
  exist" from "operation mismatch" in the message so a dead-lettered entry is actionable.

**Dropped by the parent, with reason.** The persistence lens reported `IMembershipReadRepository` as a
misnamed cross-module persistence contract. Plan §4.9's naming inventory mandates that exact name and §4.1
pre-answers the objection: *"The Read qualifier names the narrower interface's mutability; it does not
introduce another repository implementation or a different tenancy stance."* An intentional change against a
rule the plan overrode is not a finding.

**Cleared — checked and found correct.** Recording these so they are not re-derived:

- **`AudienceFor(permission)` is re-evaluated per query execution, not frozen into the cached model.** This was
  the parent's largest stated doubt. EF rewrites a closure whose static type is assignable-from the context
  type into a per-execution `__ef_filter__` parameter; both halves of the predicate satisfy that
  (`ResourceAccessExpressions.cs:18` takes `IHasResourceAccessContext`; the per-module members capture the
  concrete context). Corroborated in-repo: `TenantFilters.cs:21-25` uses the identical interface-typed capture
  and is exercised by multi-tenant integration suites on one shared host that would fail under a first-caller
  freeze. Reasoned from EF's documented behaviour, not executed — a test running the same query shape from two
  memberships in one process is still owed.
- **Predicate grouping, member/tenant audience separation, cross-scope isolation and null-caller denial** are
  all correct in all four contexts; `.And` wraps whole bodies in one `AndAlso`, so it adds no precedence hazard.
- **The incarnation check is complete** — all four columns compared on one `MembershipAuthority` row, and the
  view's aliases match the entity. `Memberships` has no soft-delete column and `(TenantId, UserId)` is unique,
  so a rejoin necessarily has a new `Id`.
- **Filter direction is one-way**; no grant filter navigates back through its parent.
- **The `ExecutionScope` machinery is fully removed** — zero remaining production references, no orphaned
  registration, and no endpoint lost an authorization attribute while surviving.
- **Seeding is sound**: seeders write only their own aggregates through the production entity path,
  `UseSeedingSupport` is present on every privileged context a seeder writes through, and keeping
  `MigrateAsync` on the filtered context is correct. `SeedIfEmptyAsync` now reads true row counts, which is a
  fix, not a regression.
- **Inbox receipt and outbox rows land on one context** in all four payment processors. The split between the
  settlement transaction and the receipt transaction is pre-existing (the candidate changed only the context
  type) and is dropped as out of scope.

**Verification run for this pass.** Solution build clean. Unit + architecture tiers: 0 failed across 14
assemblies — but see R4: that count silently excludes `Concertable.B2B.Authorization.UnitTests`. Tenant
integration: 80/83, the 3 owned by F19/F14. Lifecycle `ConcertPostingLifecycleTests`: **2 failed** (R20).
Conversations integration: **blocked by environment**, not run (R21). Application, Booking, Concert, Venue,
Artist, Admin and Process integration tiers: not run.

**Pass judgment:** `changes-requested`. Two critical findings are reachable in production (R2, R20), one
critical finding makes the candidate's headline feature silently non-functional (R1), and the verification
this candidate was committed on was narrower than reported (R4).

## Review pass — 2026-09-20 — full staged review

**Candidate base:** `7fd22b46b2270a3ab9421c2653306ed4d2c5222c`
**Candidate head:** `59f83bae674b585fd580957666387be6297e09b9`
**Candidate branch:** `Refactor/PartyFoundationLegacyBindings`
**Candidate scope:** `all`
**Candidate path-set:** `sha256:ee371cc90583f8e2c37ff31e70cf7d06304b64fd374ecb1b95ba6058f0f2bbf4` `(757 paths)`
**Candidate patch:** `sha256:769c5868315c4a561d621ecd4cc4dec21f93262d56b1fcecc7ed07b10a4cead1`
**Candidate bundle:** `C:\Users\TommySeery\source\repos\Concertable\b2b\.git\agent-workflow\runs\party-foundation-review-20260920\review\8c2ecc9249680c3af1b50c8fd7ac6df1b07ea9b8141fa7443fa9993316526744`
**Candidate bundle identity:** `sha256:9dc91b45749a31c6371e9e14c8e51ed18ba1a559000469dd8012354f5f37f24c`
**Work-order path:** `reviews/Refactor-PartyFoundationLegacyBindings.md`
**Work-order mode:** `append`
**Pass judgment:** `changes-requested`

### Staged coverage

Path assignment is by the first matching row, so every frozen manifest path belongs to exactly one stage.

- [x] **S1 — DataAccess + Authorization (70 paths):** `api/src/Concertable.B2B.DataAccess/**`, then `api/src/Modules/Authorization/**`. Native and security lenses complete; parent accepted one new finding and reconfirmed R4, R8, and R11.
- [x] **S2 — Tenant (119 paths):** `api/src/Modules/Tenant/**`. Native and security/data lenses complete; parent accepted two new findings and reconfirmed R12.
- [x] **S3 — Application + Booking + Deal + Opportunity (141 paths):** those four module roots. Native and workflow/security lenses complete; parent accepted two findings.
- [x] **S4 — Concert (109 paths):** `api/src/Modules/Concert/**`. Native and security/workflow lenses complete; parent accepted one new finding, reconfirmed R2, R3, R6, R7, R10, R13, R16 and R19, and validated R1, R5, R9, R14, R15, R20 and R23 as fixed.
- [x] **S5 — Conversations (86 paths):** `api/src/Modules/Conversations/**`. Native and security/concurrency lenses complete; parent accepted two findings and validated R21 and R22 as fixed.
- [x] **S6 — remaining backend, hosting, and tests (80 paths):** Admin, Artist, Dashboard, User, Venue, Seed, and every remaining `api/**`, `local/**`, and `tests/**` path not claimed above. Native/data and security/composition/test-impact subscopes complete; parent accepted two findings.
- [x] **S7 — shared and business clients (84 paths):** `app/shared/**`, `app/web/shared/**`, `app/web/business/**`. Native/frontend and frontend/security lenses complete; parent accepted two findings.
- [x] **S8 — profile, admin, and mobile clients (57 paths):** `app/web/admin/**`, `app/web/artist/**`, `app/web/venue/**`, `app/mobile/**`. Native/frontend and frontend/security lenses complete; parent accepted two findings.
- [x] **S9 — docs, plans, review metadata, and remaining files (11 paths):** every manifest path not claimed above. Native/docs and workflow/security lenses complete; parent reconfirmed R4 and accepted one documentation finding. Router deny hits produced no independent content finding.

### Review rules manifest

The frozen path manifest was routed through the installed Concertable skill router against the exact clean
candidate head. Applicable owners loaded by the parent reviewer: always-on instructions; package, plan,
documentation and review lifecycle; C# style/naming; result carriers/errors/terminals; module structure,
persistence, multitenancy, dependency injection, HTTP, keyed strategies, domain events, validation,
seeding, migrations, composition, unit/integration/E2E testing; TypeScript, client/server state, HTTP,
contract naming, routing, write boundaries, frontend tests and tiered sharing; the corresponding
Concertable-specific rosters; and the frozen root and nearest changed-path `AGENTS.md` files.

The router's four deny hits (`package-lock.json`, the authorization model, the party-foundation ledger,
and this review work order) remain evidence to validate in their owning stages, not automatic findings.

### Cross-area notes

Historical R1-R23 remain open until the current candidate is checked against each acceptance condition
through the address-review lifecycle. S1 reconfirmed R4 (Authorization unit-test project omitted from the
solution), R8 (non-canonical idempotency hash), and R11 (malformed tenant headers surface as 500).
S2 reconfirmed R12 (tenant-wide materialization for a membership existence test).
S4 reconfirmed R2, R3, R6, R7, R10, R13, R16 and R19. It validated R1, R5, R9, R14,
R15, R20 and R23 as fixed. R8 remains open based on the direct S1 source trace despite one S4 lens
classifying the shared implementation as repaired.
S5 validated R21 and R22 as fixed: participant expansion now uses a privileged full-ACL repository only
after filtered caller visibility succeeds, and Conversations now issues member-specific read/send grants.
Parent inspection reconfirmed R17's repeated audience predicate and validated R18 as fixed. S9's frozen
review-marker concern was discharged by this pass: the marker now names the exact reviewed head, while the
judgment stays changes-requested until address-review resolves every accepted item.

### Findings

- [x] **N1 — HIGH — security/native — automatic retry can replay a committed command after an ambiguous commit acknowledgement.**
  `api/src/Concertable.B2B.DataAccess/Concertable.B2B.DataAccess.Infrastructure/CommandExecutor.cs:33-39`
  enables the Npgsql retry strategy around the whole command transaction, while `:66-76` flushes and calls a
  plain `CommitAsync`. If PostgreSQL commits but the acknowledgement fails transiently, the strategy may run
  the mutation again. Several executor callers have no durable request receipt, so the retry can duplicate a
  mutation or outbox effect, or report a conflict after a successful write.
  **Fix:** resolve ambiguous commit success through a durable operation key and verification callback before
  replay, or require a durable idempotency key for every executor mutation; add a lost-commit-acknowledgement
  integration test.
  **Disposition:** removed whole-command automatic retries so an ambiguous or transient failure is propagated
  without replay. The focused unit and integration suites pass (5/5 and 1/1); N16 owns the missing
  commit-boundary fault coverage identified by the incremental review.

- [ ] **N2 — MEDIUM — native/security — concurrent first-tenant creation can escape as an unhandled unique-key failure.**
  `api/src/Modules/Tenant/Concertable.B2B.Tenant.Infrastructure/Repositories/TenantRepository.cs:39-54`
  selects by `CreatedByUserId FOR UPDATE`, but PostgreSQL locks no row when no tenant exists. Two first-create
  requests can therefore both pass `TenantService.cs:114-127`; the unique index at
  `TenantEntityConfiguration.cs:19` rejects one insert without translating it to the declared
  `AlreadyOwnsTenant` outcome.
  **Fix:** serialize on a stable per-user/advisory lock or classify the exact unique violation after rollback;
  add a deterministic two-request race proving one creation and stable typed/HTTP outcomes for both calls.

- [ ] **N3 — MEDIUM — native — concurrent verification reviews can both succeed and publish conflicting decisions.**
  `api/src/Modules/Tenant/Concertable.B2B.Tenant.Infrastructure/Services/VerificationService.cs:118-125`
  performs an unlocked pending-state read, transition, and save, while
  `TenantVerificationEntityConfiguration.cs:12-17` configures no concurrency token. Approve and reject can
  both observe `Pending`, both return success and notify, and the last update wins.
  **Fix:** lock the verification row for review or use optimistic concurrency and translate the loser to
  `VerificationReviewError.NotPending`; add an approve-versus-reject integration race asserting one durable
  decision and one notification.

- [x] **N4 — HIGH — workflow/security — a delayed cancellation can reopen an opportunity filled by a newer lifecycle.**
  `api/src/Modules/Opportunity/Concertable.B2B.Opportunity.Infrastructure/Events/OpportunityCancellationIntegrationEventHandler.cs:24-49`
  discards the booking/application/concert correlation and deduplicates only by envelope message id before
  unconditionally reopening the current `Filled` opportunity. `OpportunityEntity.cs:13-50` stores no fill
  incarnation. A distinct delayed cancellation from lifecycle A can therefore arrive after lifecycle B
  refills the opportunity and reopen B's live slot.
  **Fix:** persist the current fill's application/lifecycle correlation when `MarkFilled` runs and reopen only
  when the cancellation matches it; test cancel A, refill with B, then deliver a fresh-message-id cancellation
  for A and assert the opportunity remains filled.
  **Disposition:** accepted events now carry `ApplicationId`, Opportunity persists it for the active fill, and
  both cancellation event types reopen only a matching fill. The refreshed Opportunity migration has no drift,
  the project builds cleanly, and all three focused integration regressions pass, including delayed A after B.

- [ ] **N5 — LOW — native — Application read paths drop request cancellation before database and cross-module I/O.**
  `api/src/Modules/Application/Concertable.B2B.Application.Infrastructure/Services/ApplicationService.cs:75-160`
  exposes summary, proposal, opportunity, and dashboard reads without a cancellation token and calls
  repositories/modules without one.
  **Fix:** thread `CancellationToken ct = default` through interface, implementation and controller actions,
  pass it to every supporting I/O call, and cover propagation on a representative endpoint.

- [ ] **N6 — MEDIUM — security — concurrent first invoice-number allocation is not serialized.**
  `api/src/Modules/Concert/Concertable.B2B.Concert.Infrastructure/Repositories/InvoiceSequenceRepository.cs:20-28`
  selects the supplier sequence `FOR UPDATE`, but PostgreSQL locks no row when the sequence does not exist;
  `InvoiceIssuer.cs:53-58` then creates it. Two first invoices for one supplier can both allocate `000001`,
  with one later failing a constraint and aborting settlement processing.
  **Fix:** serialize on a stable supplier/advisory key or pre-provision the sequence, or classify and retry the
  exact creation race; add a two-concert first-invoice concurrency test proving unique monotonic numbers.

- [ ] **N7 — MEDIUM — native/security — concurrent first conversation creation can violate request-id replay semantics.**
  `api/src/Modules/Conversations/Concertable.B2B.Conversations.Infrastructure/Repositories/ConversationRepository.cs:49-67`
  locks an absent receipt key, which PostgreSQL cannot serialize. Two same-actor/same-request calls can both
  create before the unique receipt constraint rejects one; `ConversationService.cs:102-128` has no duplicate
  classification/replay path, so the loser receives a database error.
  **Fix:** serialize on a stable actor/request key or classify the exact duplicate after rollback, re-read the
  winning receipt, and return replay/`RequestConflict`; add a genuinely concurrent integration race.

- [ ] **N8 — LOW — test-impact/security — assigned-member messaging authority lacks integration coverage.**
  `ConversationEntity.cs:43-74` and `ConversationService.cs:264-345` add security-sensitive assignment and
  removal behavior, but only aggregate unit coverage exists.
  **Fix:** add API coverage for assigned Staff read/send access, unassigned denial, removal, cross-tenant
  rejection, stale access version, and membership removal/rejoin not inheriting the old grant.

- [ ] **N9 — MEDIUM — seeding/composition — integration setup still inserts B2B users directly.**
  `api/src/Modules/User/Concertable.B2B.User.Infrastructure/Extensions/ServiceCollectionExtensions.cs:62-65`
  registers `UserTestSeeder`, which inserts `SeedState.Users` directly at `UserTestSeeder.cs:23-29` even
  though production creates users only through `CredentialRegisteredEvent`. This can keep integration startup
  green while credential registration or same-flow admin provisioning is broken.
  **Fix:** provision test identity through the real registration handler, preserve the resulting rows across
  Respawn, and assert registration produces both the B2B user and eligible admin profile.

- [ ] **N10 — MEDIUM — E2E — the admin concert-id query uses unquoted PostgreSQL identifiers.**
  `tests/E2ETests/Concertable.B2B.E2ETests.Server/E2EAdminExtensions.cs:176-181` queries
  `SELECT Id FROM concert.Concerts WHERE ApplicationId = ...`, which PostgreSQL folds to lowercase and cannot
  resolve against the quoted PascalCase schema used by the migration and adjacent queries.
  **Fix:** quote schema objects and columns consistently and add an authenticated E2E-admin endpoint smoke
  assertion against seeded data.

- [ ] **N11 — MEDIUM — frontend — losing the final membership leaves a stale active tenant persisted.**
  `app/shared/src/features/tenant/hooks/useTenant.ts:17-19` skips reconciliation when memberships becomes
  empty, although `useTenantStore.ts:28-42` would clear an invalid selection. The stale tenant remains in
  Zustand/local storage until another route resolution or logout.
  **Fix:** reconcile or explicitly clear on the empty-membership transition and test one-membership-to-zero
  behavior across store and persistence.

- [ ] **N12 — MEDIUM — frontend contract — membership controls render an unsupported role.**
  `app/shared/src/features/tenant/constants.ts:10` exports `restrictedParticipant`,
  `app/web/shared/src/features/tenant/constants.ts:10` labels it, and `MembersRoster.tsx:68` renders it even
  though the backend `TenantRole` enum has no such member. An authorized user can submit a role the API cannot
  deserialize.
  **Fix:** remove the stale role from the frontend catalog or implement it end-to-end, and prove every rendered
  role is backend-supported.

- [ ] **N13 — MEDIUM — mobile permissions — Operations is exposed without `operations.view`.**
  `app/mobile/src/navigation/BusinessNavigator.tsx:33-41` registers the Operations tab for every tenant member
  even though the navigator already receives the active permission set; the Messages tab is correctly gated.
  **Fix:** register Operations only when `permissions.has("operations.view")` and test tab absence/presence.

- [ ] **N14 — MEDIUM — mobile authentication — unauthenticated sessions mount account-owned navigation.**
  `app/mobile/src/navigation/RootNavigator.tsx:148-153` sends `user === undefined` to `ArtistTabs`, which
  unconditionally mounts My Artist, Messages, and Profile stacks at `ArtistTabs.tsx:35-59`.
  **Fix:** use a dedicated public/auth navigator or gate every account-owned tab on authentication, and test
  that private routes are absent for an unauthenticated session.

- [ ] **N15 — MEDIUM — docs/workflow — the roadmap's status prose contradicts the progress ledger.**
  `plans/party-foundation/PARTY_FOUNDATION_ROADMAP.md:6-10` says no implementation phase is delivered while
  `PARTY_FOUNDATION_PROGRESS.md:16-18` records P1 implementation and qualification.
  **Fix:** state that P1 is implemented and under review while later phases and the overall roadmap item
  remain pending, so the durable index and ledger agree.

All 757 manifest paths were covered serially. Accepted new findings: 15 (2 high, 11 medium, 2 low).
Historical findings still open after current-head validation: R2, R3, R4, R6, R7, R8, R10, R11, R12,
R13, R16, R17 and R19. Historical findings validated fixed: R1, R5, R9, R14, R15, R18, R20, R21, R22
and R23. Address-review owns the next state transition.

## Review pass — 2026-09-20 — incremental

**Candidate base:** `59f83bae674b585fd580957666387be6297e09b9`
**Candidate head:** `6b980063785c1f6f3aaa232137fdd6662743e695`
**Candidate branch:** `Refactor/PartyFoundationLegacyBindings`
**Candidate scope:** `all`
**Candidate path-set:** `sha256:9193afd91a05755d82e4499762281f1fc79d22c125a10d813a1e696d7446bf2e` `(2 paths)`
**Candidate bundle:** `C:\Users\TommySeery\source\repos\Concertable\b2b\.git\agent-workflow\runs\party-foundation-remediation-20260920\review\incremental-6b980063785c1f6f3aaa232137fdd6662743e695`
**Candidate bundle identity:** `sha256:f69bda228c1d7f230bb0695e04f0847a134bc9f1d1cf606aee22d13333bcb5d6`
**Work-order path:** `reviews/Refactor-PartyFoundationLegacyBindings.md`
**Work-order mode:** `append`
**Pass judgment:** `approved`

### Findings

No new findings. Native and security lenses verified the exact two-path frozen delta and found no replacement
HTTP reachability for concert completion.

## Review pass — 2026-09-20 — incremental

**Candidate base:** `6b980063785c1f6f3aaa232137fdd6662743e695`
**Candidate head:** `a3aeb23668404ccee4997c41d20f239c3af3b39c`
**Candidate branch:** `Refactor/PartyFoundationLegacyBindings`
**Candidate scope:** `all`
**Candidate path-set:** `sha256:6536254f1e6566ee9123139bc73b62f58ff1dc2ac8ea475b6d697a0e8a422a55` `(8 paths)`
**Candidate bundle:** `C:\Users\TommySeery\source\repos\Concertable\b2b\.git\agent-workflow\runs\party-foundation-remediation-20260920\review\incremental-a3aeb23668404ccee4997c41d20f239c3af3b39c`
**Candidate bundle identity:** `sha256:82c3795b687f33e2ea60ead95ede1a9a0373b56ad8ca5860990cdc58277e867e`
**Work-order path:** `reviews/Refactor-PartyFoundationLegacyBindings.md`
**Work-order mode:** `append`
**Pass judgment:** `changes-requested`

### Findings

- [x] **N16 — MEDIUM — native/security — the N1 regression does not exercise an ambiguous commit acknowledgement.**
  `CommandExecutionApiTests.cs:32-39` throws its transient `NpgsqlException` from the command delegate, before
  `CommandExecutor` reaches flush, authority validation, or `CommitAsync`. It proves only that a command-body
  failure is not retried, while the N1 disposition claimed commit-boundary coverage.
  **Fix:** fault the actual commit acknowledgement after a real durable write, assert the command ran once and
  its durable effect exists once, and verify the ambiguous exception is surfaced.
  **Disposition:** added an internal commit coordinator and a fixture-owned implementation that commits a real
  PostgreSQL probe row before throwing the lost acknowledgement. The regression passes and proves one command
  invocation, one durable row, and the surfaced transient exception.

## Review pass — 2026-09-20 — incremental

**Candidate base:** `a3aeb23668404ccee4997c41d20f239c3af3b39c`
**Candidate head:** `026f9892163df302262d915f54165e0c825034c0`
**Candidate branch:** `Refactor/PartyFoundationLegacyBindings`
**Candidate scope:** `all`
**Candidate path-set:** `sha256:a1226262b64561ba1f464e668489dbaa08f4a97e7606c2e0e2f98f77e6cb86ec` `(7 paths)`
**Candidate bundle:** `C:\Users\TommySeery\source\repos\Concertable\b2b\.git\agent-workflow\runs\party-foundation-remediation-20260920\review\incremental-026f9892163df302262d915f54165e0c825034c0`
**Candidate bundle identity:** `sha256:851fbe50eacc90c06ec10f326829e2e30dba931285b7dd80fd061acb1525ffb0`
**Work-order path:** `reviews/Refactor-PartyFoundationLegacyBindings.md`
**Work-order mode:** `append`
**Pass judgment:** `changes-requested`

### Findings

- [x] **N17 — MEDIUM — security — ambiguous physical commit failure can be masked by rollback cleanup.**
  The fixture fault runs only after `CommandTransaction.CommitAsync` has released participants and set
  `completed`. A real lost acknowledgement throws from the inner Npgsql commit before those state changes, so
  `CommandExecutor` attempts rollback and disposal can replace the original exception on an already committed
  or broken transaction.
  **Fix:** fault the physical commit boundary, track commit-attempted separately from completed, never roll back
  after commit begins, preserve the original exception through best-effort cleanup, and retain the one-command,
  one-durable-row regression.
  **Disposition:** the fault seam now wraps the physical Npgsql commit. `CommandTransaction` records commit
  attempted before I/O, suppresses rollback thereafter, and makes participant and connection cleanup
  best-effort so the original ambiguity survives. The real-database regression and DataAccess unit suite pass
  (1/1 and 5/5).

## Review pass — 2026-09-20 — incremental

**Candidate base:** `026f9892163df302262d915f54165e0c825034c0`
**Candidate head:** `b4e4dcc3e3c26f606956d7135fcce3483fce8631`
**Candidate branch:** `Refactor/PartyFoundationLegacyBindings`
**Candidate scope:** `all`
**Candidate path-set:** `sha256:0ce65e0b1082fc6f7d1a37225ecdde3657363d592e6883aa636e81352eb1ecb7` `(7 paths)`
**Candidate bundle:** `C:\Users\TommySeery\source\repos\Concertable\b2b\.git\agent-workflow\runs\party-foundation-remediation-20260920\review\incremental-b4e4dcc3e3c26f606956d7135fcce3483fce8631`
**Candidate bundle identity:** `sha256:4813fb36b4aef8c981bf15b1fc961ac592b0a0d5a328bb7bd82d165a7b321aa2`
**Work-order path:** `reviews/Refactor-PartyFoundationLegacyBindings.md`
**Work-order mode:** `append`
**Pass judgment:** `approved`

### Findings

No new findings. Native and security lenses verified that the physical-commit state machine closes N17 and
that the original ambiguous exception survives best-effort cleanup without replay or rollback.

## Review pass — 2026-09-20 — incremental

**Candidate base:** `b4e4dcc3e3c26f606956d7135fcce3483fce8631`
**Candidate head:** `d28e3412514c42568a6d3ab90abd01f321012334`
**Candidate branch:** `Refactor/PartyFoundationLegacyBindings`
**Candidate scope:** `all`
**Candidate path-set:** `sha256:55cc34e425477165a69052e14c9423fa94f934f323af12f34ce6a16dc4fb2c31` `(10 paths)`
**Candidate bundle:** `C:\Users\TommySeery\source\repos\Concertable\b2b\.git\agent-workflow\runs\party-foundation-remediation-20260920\review\incremental-d28e3412514c42568a6d3ab90abd01f321012334`
**Candidate bundle identity:** `sha256:38f482feed261fc8fbc7522679142f04becd0c37ee79134a1aa929a7e37b07f5`
**Work-order path:** `reviews/Refactor-PartyFoundationLegacyBindings.md`
**Work-order mode:** `append`
**Pass judgment:** `changes-requested`

### Findings

The native lens found no issues with the application correlation, guarded reopen transition, migration,
or delayed-cancellation regression. The security lens accepted one residual lifecycle-ordering finding.

- [x] **N18 — HIGH — security — cancellation before acceptance can refill a cancelled application.**
  A fresh cancellation envelope is durably inboxed while an open opportunity forgets the application-level
  cancellation. A delayed or concurrently committed acceptance envelope can then fill the opportunity for
  that cancelled application.
  **Fix:** retain per-application cancellation state, serialize both lifecycle handlers on the opportunity
  row, and add reversed-order and concurrent real-database regressions.
  **Disposition:** the opportunity now persists a distinct cancelled-application history; acceptance refuses
  those application IDs, and both handlers lock the same opportunity row before applying the transition.
  The re-scaffolded InitialCreate has no model drift, the focused graph builds with zero warnings/errors, and
  all five cancellation-handler integration tests pass, including reversed and concurrent delivery.

## Review pass — 2026-09-20 — incremental

**Candidate base:** `d28e3412514c42568a6d3ab90abd01f321012334`
**Candidate head:** `692aff71828f7b6be69182bd99b5d6ef7e6a170e`
**Candidate branch:** `Refactor/PartyFoundationLegacyBindings`
**Candidate scope:** `all`
**Candidate path-set:** `sha256:60f393196cb1e29a2490fb13658f0384fbf0ad567696228fa960ad33acd5d578` `(9 paths)`
**Candidate bundle:** `C:\Users\TommySeery\source\repos\Concertable\b2b\.git\agent-workflow\runs\party-foundation-remediation-20260920\review\incremental-692aff71828f7b6be69182bd99b5d6ef7e6a170e`
**Candidate bundle identity:** `sha256:1f74766eb5568472ec204928ffa133952f3fe67057bcb3cb8716763175197c59`
**Work-order path:** `reviews/Refactor-PartyFoundationLegacyBindings.md`
**Work-order mode:** `append`
**Pass judgment:** `changes-requested`

### Findings

Both lenses accepted the same test-quality finding and found no production, authority, inbox, transaction,
or migration defect.

- [x] **N19 — MEDIUM — test-quality — the concurrent lifecycle regression does not force overlap.**
  `Task.WhenAll` permits either handler to finish before the other reaches its row lock. Both serial orders
  produce the expected final state, so the test can stay green if the locks regress and the lost-update window
  returns.
  **Fix:** pause the first handler after it has read and mutated under its transaction, start the competing
  handler on another scope/connection, prove it cannot complete until the first operation is released, then
  assert the final terminal state.
  **Disposition:** Opportunity now carries the repository-standard PostgreSQL `xmin` concurrency token. The
  integration fixture's conflict interceptor pauses each first handler immediately after its locked entity
  read, starts the inverse handler on a separate scope, and asserts it remains blocked until release. Both
  orderings converge to the cancelled/open state; all six focused tests pass, the build is warning-free, and
  the re-scaffolded InitialCreate has no model drift.

## Review pass — 2026-09-20 — incremental

**Candidate base:** `692aff71828f7b6be69182bd99b5d6ef7e6a170e`
**Candidate head:** `bc820580013cd3ce227e2144ce40abdd79b16eee`
**Candidate branch:** `Refactor/PartyFoundationLegacyBindings`
**Candidate scope:** `all`
**Candidate path-set:** `sha256:4e1aa9c23e6e319b29f16f3c4d3077d4946a592938a6eb440662283e20ff1f5d` `(9 paths)`
**Candidate bundle:** `C:\Users\TommySeery\source\repos\Concertable\b2b\.git\agent-workflow\runs\party-foundation-remediation-20260920\review\incremental-bc820580013cd3ce227e2144ce40abdd79b16eee`
**Candidate bundle identity:** `sha256:a005d10b903a7edc64f94e5b9062648c3320ec5528fafa54543c577bc7b7e579`
**Work-order path:** `reviews/Refactor-PartyFoundationLegacyBindings.md`
**Work-order mode:** `append`
**Pass judgment:** `changes-requested`

### Findings

The lenses accepted two coupled defects in the concurrency-test mechanism. Neither found a defect in the
production lifecycle lock or persisted terminal-state implementation.

- [x] **N20 — MEDIUM — test-quality — timeout alone does not prove the competing lock was submitted.**
  A cold runner can spend the timeout resolving the competing handler or opening its connection. The test can
  then pass through serial execution even without `FOR UPDATE` because both serial orders reach the asserted
  final state.
  **Fix:** signal from the competing connection when its `FOR UPDATE` command is submitted, assert the handler
  remains incomplete only after that signal, then release the first operation and verify completion/state.
  **Disposition:** added a test-only command interceptor that starts the competitor after the first handler owns
  the row lock, signals from the competing raw lock command's non-query interception path, and verifies that the
  competing handler is incomplete only after command submission. Both race orderings pass deterministically.

- [x] **N21 — MEDIUM — correctness — adding Opportunity `xmin` broadens concurrency without a stable edit terminal.**
  The ordinary venue edit reads before its unit of work and has no typed conflict translation or retry. A
  concurrent lifecycle event can therefore surface an unhandled `DbUpdateConcurrencyException` as a 500.
  **Fix:** keep the lifecycle synchronization seam test-only and remove the new production concurrency token.
  **Disposition:** removed the Opportunity concurrency interface, version property, mapping, and dependency. The
  re-scaffolded initial migration has no `xmin` column, and the Opportunity pending-model-change gate is clean.

## Review pass — 2026-09-20 — incremental

**Candidate base:** `bc820580013cd3ce227e2144ce40abdd79b16eee`
**Candidate head:** `77da6bb794521778b4454b0354a68373f37e2a25`
**Candidate branch:** `Refactor/PartyFoundationLegacyBindings`
**Candidate scope:** `all`
**Candidate path-set:** `sha256:7e0d518eb5c358f46ca043118590596c59c8d8d7a0d00647195d2bde3ceb90a1` `(10 paths)`
**Candidate bundle:** `C:\Users\TommySeery\source\repos\Concertable\b2b\.git\agent-workflow\runs\party-foundation-remediation-20260920\review\incremental-77da6bb794521778b4454b0354a68373f37e2a25`
**Candidate bundle identity:** `sha256:7bc6ddd0b0b2c503ca249df17f76e12288b39fa6115bdd8291d629d53551e245`
**Work-order path:** `reviews/Refactor-PartyFoundationLegacyBindings.md`
**Work-order mode:** `append`
**Pass judgment:** `changes-requested`

### Findings

Both lenses accepted the production `xmin` rollback and found no production, privilege/isolation, reset,
or migration defect. They independently identified the same remaining weakness in the contention proof.

- [x] **N22 — MEDIUM — test-quality — the lock signal fires before PostgreSQL can observe contention.**
  EF's `ReaderExecutingAsync` and `NonQueryExecutingAsync` callbacks run before ADO sends the command, so the
  immediate incomplete-task assertion is tautological at interception time and does not prove a database lock
  wait occurred.
  **Fix:** keep the first transaction paused until an independent connection observes the competing backend in
  a PostgreSQL `Lock` wait, then assert the competing task remains incomplete, release, and verify final state.
  **Disposition:** the interceptor now captures the competing connection's backend process id, then polls
  `pg_stat_activity` through an independent data-source connection until PostgreSQL reports `wait_event_type =
  'Lock'`. Only that server-side observation releases the first update. Both race orderings pass in the exact
  six-test suite, and the focused build remains warning-free.

## Review pass — 2026-09-20 — incremental

**Candidate base:** `77da6bb794521778b4454b0354a68373f37e2a25`
**Candidate head:** `368900e9492d9c6b166ba66df9161c2c2e055609`
**Candidate branch:** `Refactor/PartyFoundationLegacyBindings`
**Candidate scope:** `all`
**Candidate path-set:** `sha256:e11d7e7af99d8654ab89fe99befdcd52cafbe4b4948435dd227d97d2e44b4d90` `(4 paths)`
**Candidate bundle:** `C:\Users\TommySeery\source\repos\Concertable\b2b\.git\agent-workflow\runs\party-foundation-remediation-20260920\review\incremental-368900e9492d9c6b166ba66df9161c2c2e055609`
**Candidate bundle identity:** `sha256:c978b84ecd237668f559e6194f850ef78148427e364e7970a0761f729074dbd7`
**Work-order path:** `reviews/Refactor-PartyFoundationLegacyBindings.md`
**Work-order mode:** `append`
**Pass judgment:** `changes-requested`

### Findings

The native/general lens was clean. The security/concurrency lens confirmed the server-observed lock proof and
accepted the PID, observer-connection, and PostgreSQL visibility semantics, but found one cleanup gap.

- [x] **N23 — LOW — test-isolation — a failed contention assertion can leak the detached competitor.**
  The helper awaits the competitor only after the first handler succeeds. If the server observation times out
  or the first handler fails, its transaction rolls back and the unobserved competitor can resume during the
  next fixture reset.
  **Fix:** preserve the first failure, cancel the competitor, and always bounded-await/observe it in `finally`
  after the first transaction unwinds before rethrowing the original failure.
  **Disposition:** the helper now gives the competitor its own cancellation token, preserves any failure from
  the first handler, and always cancels plus bounded-awaits the competitor in `finally` before rethrowing that
  original failure with its stack intact. The exact six-test suite passes and the build is warning-free.

## Review pass — 2026-09-20 — incremental

**Candidate base:** `368900e9492d9c6b166ba66df9161c2c2e055609`
**Candidate head:** `3ffe856c77a59c960218e9816f6b591a04cd7712`
**Candidate branch:** `Refactor/PartyFoundationLegacyBindings`
**Candidate scope:** `all`
**Candidate path-set:** `sha256:000db31e054a6cadc3d4be61b58814a67f30c6010473d1813dd3a17121dd97b0` `(2 paths)`
**Candidate bundle:** `C:\Users\TommySeery\source\repos\Concertable\b2b\.git\agent-workflow\runs\party-foundation-remediation-20260920\review\incremental-3ffe856c77a59c960218e9816f6b591a04cd7712`
**Candidate bundle identity:** `sha256:c1fc9472cbccb599a052e60b4095a7771b61a5574bcea179247ea7fae139fb8d`
**Work-order path:** `reviews/Refactor-PartyFoundationLegacyBindings.md`
**Work-order mode:** `append`
**Pass judgment:** `approved`

### Findings

Both native/general and security/concurrency lenses found no actionable issue. N23 is closed without weakening
the server-observed contention proof.
