# Code review — Refactor/PartyFoundationLegacyBindings

> **This file is a work order, not a discussion.** If you're handed this file, fix the open `[ ]`
> findings directly and report what changed. Tick each `[x]` as you land it. Pause only for a genuinely
> irreversible or ambiguous finding: record its durable disposition, take the safe path, and keep going.

**Review status:** `in-progress`
**Reviewed up to commit:** `bd9e45558b67b088fdd5f7bfe0eb9e223817e422`  `(2026-09-21)`
**Security-reviewed up to commit:** `bd9e45558b67b088fdd5f7bfe0eb9e223817e422`  `(2026-09-21)`
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

- [x] **R1 — critical — every Concert ACL command decides against the caller-visible grant subset, not the ACL.**
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
  **Disposition:** all five command paths now lock and load the minimal concert identity, establish resource
  authority, and only then load the complete ACL through the privileged repository under the same transaction.
  The receipt repository, privileged unit of work, and grant writes share `ConcertPrivilegedDbContext`. A new
  integration regression exercises revoke, assign, and remove against a concert the caller does not own and
  proves all three return 403. Three service-level repository-spy regressions additionally prove the full ACL
  load is never invoked after `CanShareAsync` denies authority. Those tests pass 3/3, the full Concert unit suite
  passes 96/96, and the full access-control integration class passes 9/9.

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
  duplicate-grant case to `AlreadyShared`. The existing receipt configuration is now registered so its
  tenant/operation/request unique index is present in the re-scaffolded InitialCreate. A PostgreSQL advisory-lock
  barrier forces two independent concert commands to reach the receipt insert together; it proves one success,
  one recovered `RequestConflict`, and successful database work from the recovered scope afterward. The exact
  barrier regression passes twice, the three recovery mappings pass, the access class passes 7/7, Concert unit
  tests pass 93/93, and all eleven migration snapshots have no drift.

## Review pass — 2026-09-20 — incremental

**Candidate base:** `8eacaf0130a88e265d604f224fdf462d80eeb936`
**Candidate head:** `a412723de9060b65e5c715f3350d4ead634f032b`
**Candidate branch:** `Refactor/PartyFoundationLegacyBindings`
**Candidate scope:** `all`
**Candidate path-set:** `sha256:fe507895ad3f94aaff14f0bb39bf58e20a8b9e9e5aef115d20535af2f45ee742` `(2 paths)`
**Candidate bundle:** `C:\Users\TommySeery\source\repos\Concertable\b2b\.git\agent-workflow\runs\party-foundation-remediation-20260920\review\incremental-a412723de9060b65e5c715f3350d4ead634f032b`
**Candidate bundle identity:** `sha256:2b99ba112d46feb0f6106f2263cd88f88fcb2dea68ddcec384e9fe116693aff9`
**Work-order path:** `reviews/Refactor-PartyFoundationLegacyBindings.md`
**Work-order mode:** `append`
**Pass judgment:** `changes-requested`

### Findings

Both lenses found one medium regression gap: the cited same-concert race serializes on the concert row lock
and does not prove the new duplicate-key recovery branch. Deterministic PostgreSQL barrier coverage and exact
recovery-mapping tests are required before R7 can close.

- [x] **R8 — medium — `ResourceCommandReceipt.HashPayload` is not injective and is not `DateTimeKind`- or
  culture-stable.** `ResourceCommandReceipt.cs:42-54`: the `U+001F` separator is neither escaped nor
  length-prefixed, so `("aU+001Fb","c")` and `("a","bU+001Fc")` collide; the `null` sentinel `"U+0000"`
  collides with a literal `"U+0000"`; `part.ToString()` uses the current culture; and `DateTime.ToString("O")`
  encodes `Kind`, so the same instant sent as `…Z` and `…+01:00` hashes differently and a genuine retry gets
  `RequestConflict`. The last case is reachable today through `request.ValidUntil`.
  **Fix:** length-prefix each part, normalise `DateTime` with `ToUniversalTime()`, and format every
  `IFormattable` with `CultureInfo.InvariantCulture`.
  **Disposition:** payload hashing now streams a signed big-endian length before each UTF-8 part, reserves `-1`
  for null, normalizes `DateTime` to UTC round-trip text, and formats every other `IFormattable` invariantly.
  Focused tests prove separator partitions and null/literal-null cannot collide, decimal formatting is stable
  across British and French cultures, and UTC/local representations of one instant hash equally. DataAccess unit
  tests pass 9/9, Conversations idempotency integrations pass 7/7, and Concert access integrations pass 8/8.

- [x] **R9 — medium — replay maps a server-side inconsistency to `ConcertNotFound` for a concert it just
  loaded, permanently.** `ConcertService.cs:369-376` returns `ConcertNotFound` (404) after the concert loaded
  successfully, when the receipt's recorded grant cannot be resolved. Because the receipt is durable, every
  later retry of that `RequestId` takes the same branch.
  **Fix:** treat an unresolvable recorded outcome as an invariant violation (throw, as
  `SettlementPaymentProcessor.cs:51-59` does) or give it its own error arm. Decide separately what a replay
  should report when the grant was since revoked.
  **Disposition:** replay returns `RequestConflict` only when the request payload differs. A matching durable
  receipt with a malformed outcome or a grant that cannot be resolved from the complete ACL now raises an
  invariant failure instead of blaming the client or reporting the concert missing. A focused unit regression
  supplies a matching receipt whose recorded grant is absent and asserts the receipt and grant identities are
  preserved in the thrown diagnostic; a second regression covers the malformed-outcome invariant and receipt
  identity. The replay-classification tests pass 5/5, all Concert unit tests pass 98/98, and the full
  access-control integration class passes 9/9.

- [x] **R10 — medium — cross-tenant existence probes run before the caller's authority over the concert is
  established.** `ConcertService.cs:313-318` calls `tenantModule.GetByIdAsync` and `IsCurrentMembershipAsync`
  on caller-supplied GUIDs before loading the concert at `:320`. Any holder of `resources.share` gets a
  distinguishable `InvalidRecipient` for a concert id they hold no grant on. Bounded by v4 GUID space, so an
  ordering defect rather than a usable oracle.
  **Fix:** load the concert and run its `NotFound`/`Superseded` checks first.
  **Disposition:** summary sharing now revalidates the actor, locks the concert identity, establishes share
  authority, loads the complete ACL, resolves any durable replay, and checks the expected access version before
  probing recipient existence. Duplicate recovery no longer probes the recipient because it only classifies
  already-persisted outcomes. The focused integration regression uses a nonexistent recipient against a concert
  the caller does not control and now returns 403 instead of disclosing recipient validity; it passes 1/1 and
  the full access-control class passes 9/9.

- [x] **R11 — medium — `MalformedTenantHeaderException` is mapped nowhere.**
  `MembershipContext.cs:71-72` throws it from `TenantResolutionMiddleware`, which runs for the whole pipeline
  (`B2BWebHostExtensions.cs:256`). The only two references in the tree are the throw and the declaration — no
  handler, no `ProblemDetails` arm — so it conventionally surfaces as 500 for what is a client error. It also
  fires for *duplicate* `X-Tenant-Id` headers, because `TryGetHeaderTenantId` parses `values.ToString()`
  (`MembershipContext.cs:84-86`), which comma-joins. Not reachable pre-authentication:
  `MembershipContext.cs:51-55` returns for an anonymous caller before any header parsing.
  **Fix:** map it to 400 at the Web host, or short-circuit in the middleware.
  **Disposition:** a Web exception handler registered before the global fallback now maps only
  `MalformedTenantHeaderException` to a 400 Problem Details response. Real-pipeline regressions prove both an
  invalid value and duplicate `X-Tenant-Id` values return 400. The focused regressions pass 2/2, the full
  active-tenant resolution class passes 7/7, and the Tenant integration project passes 92/92.

- [x] **R12 — medium — `IsCurrentMembershipAsync` materialises every membership of a tenant to answer one
  boolean, on a request path.** `TenantService.cs:57-61` calls `ListMembershipsByTenantAsync` (tracked entities,
  `MembershipRepository.cs:43-44`) then filters in memory. It runs before every share and every member
  assignment (`ConcertService.cs:317,413`). The same repository already shows the right shape at `:52-53`.
  **Fix:** add `ExistsByTenantIdAndIdAsync` as an `AnyAsync` and call it.
  **Disposition:** R10 removed the final Concert share and member-assignment consumers, so incremental review
  rejected an optimized implementation of this now-dead API. The unused module/service contract and forwarding
  chain is deleted instead; the tenant-wide tracked query remains only for actual members-management lists.
  The Tenant unit suite passes 153/153, the Concert access-control integration class passes 9/9, and both
  affected projects build with zero warnings and errors.

- [x] **R13 — medium — `IInvoiceSequenceRepository.InsertAsync` has `AddAsync` semantics.**
  `InvoiceSequenceRepository.cs:19-20` stages without saving, while the persistence standard fixes
  `InsertAsync` as stage-and-save and every inherited implementation in the codebase saves.
  **Fix:** rename to `AddAsync` on interface and implementation.
  **Disposition:** renamed the interface, implementation and `InvoiceIssuer` call site to `AddAsync`, preserving
  the intended stage-only write inside the issuer's owning unit of work. The Concert integration project builds
  with zero warnings and errors, and all 13 `ConcertInvoiceApiTests` pass.

- [x] **R14 — medium — two registered services have no consumer and the code they replace is unchanged.**
  `IInvoicePrivilegedRepository` and `IInvoiceSequenceRepository` are registered
  (`ServiceCollectionExtensions.cs:84-85`) with no injection site, while `InvoiceIssuer` still does all three
  jobs against the context directly (`InvoiceIssuer.cs:26,50-51,55,70`). Plan §4.6 requires `InvoiceIssuer` to
  take those repositories and drop its `DbContext` parameter; this candidate created them and stopped.
  **Fix:** wire `InvoiceIssuer` to them (this is 4.5/4.6 work and may be deferred to that slice, but the
  registrations should not sit dead in the meantime).
  **Disposition:** the current `InvoiceIssuer` constructor injects both repositories and uses them for the
  booking existence check, supplier-scoped sequence lock/allocation, sequence staging and invoice staging;
  it no longer accepts or uses `ConcertDbContext`. The Concert integration project builds cleanly and all 13
  `ConcertInvoiceApiTests` pass.

- [x] **R15 — medium — the `IsHost` bypass shape survives on two live predicates.**
  `TenantFilters.cs:24` still ORs `context.TenantContext.IsHost` into every single-owner filter, and
  `ConcertService.cs:273` still ANDs `!tenantContext.IsHost`. Both are inert only because both implementations
  hard-code `false` (`MembershipContext.cs:36`, `DesignTimeTenantContext.cs:12`). `ITenantContext` is a
  platform package type, so the member cannot be removed here — but the disjuncts can, and two unit tests
  already mock it `true` (`ConcertServiceTests.cs:117,157`), asserting through a bypass production cannot
  reach.
  **Fix:** drop both local disjuncts and the two `true` mocks.
  **Disposition:** no `IsHost` reference remains in the repository. Single-owner filters now compare only the
  entity and active tenant ids, and Concert action predicates require actual party ownership. All four
  `ResourceAccessGuardTests` and all nine `ConcertAccessApiTests` pass on the current graph.

- [x] **R16 — low — `AssignMember`'s tenant guard is vacuous.** `ConcertEntity.cs:181-185` compares
  `membershipTenantId != actorTenantId`, and the sole caller passes `actor.TenantId` for both
  (`ConcertService.cs:423`). The real check is `IsCurrentMembershipAsync` at `:413`; the domain guard looks
  like a second barrier and is not.
  **Fix:** drop the parameter and the comparison, or pass the membership's actual owning tenant.
  **Disposition:** removed the redundant `membershipTenantId` parameter and comparison from the domain method
  and its sole caller. Target membership validity remains established by `TenantCommandFactsResolver` for the
  actor tenant before the domain call. The full Concert unit suite passes 98/98 and all nine
  `ConcertAccessApiTests` pass.

- [x] **R17 — low — the shared audience predicate is copy-pasted ten times across four contexts.**
  `ConcertDbContext.cs:43-46,48-51,57-60`; `BookingDbContext.cs:29-32,38-41`;
  `ApplicationDbContext.cs:31-34,36-39`; `ConversationsDbContext.cs:36-39,41-44`. Plan §4.2 requires the
  shared membership/audience/time expression to be expressed once in DataAccess's expression builder;
  `ResourceAccessExpressions` factored out the membership/validity half and stopped, leaving the
  security-critical `MembershipId == null || == ActiveMembershipId` rule with ten edit sites.
  **Fix:** add an `Or` combinator and a `ReachableAtAudience<TGrant, TScope>(context, permission)` expression;
  each context then supplies only its scope test.
  **Disposition:** added `LiveForAudience` and `Or` to the shared expression builder and reduced all four
  contexts to their scope-to-audience mapping. No context retains a copied
  `TenantResources`/`AssignedResources` clause. The clean solution build has zero warnings and errors;
  resource-access architecture tests pass 4/4; the Application, Booking, Concert and Conversations integration
  suites pass 76/76, 24/24, 83/83 and 17/17.

- [x] **R18 — low — dangling doc references to the deleted `AccessScopedDbContext`.**
  `TenantScopedDbContext.cs:13,53`, `TenantFilters.cs:10` and `CODE_PATTERNS.md:14,20` still name the type this
  candidate deletes. The two source files are outside the changed-path set, so the candidate broke references
  in unchanged files — the rename's grep gate was not run.
  **Fix:** rename all five to `ResourceScopedDbContext` and run `grep -rniE "accessscopeddbcontext|accesscontext"`
  to zero.
  **Disposition:** current source and guidance use `ResourceScopedDbContext`; `CODE_PATTERNS.md` names it for
  both the grant-reached roster and inheritance relationship. `AccessScopedDbContext` remains only in the
  historical plan rename table and this finding record, not in live source or guidance.

- [x] **R19 — low — stray U+FEFF mid-file.** `ConcertServiceTests.cs:3` and `ConcertServiceCreateTests.cs:3`
  (byte offset 87 in both) carry a BOM in the middle of the file, from prepending `using` lines above a
  BOM-bearing first line. Compiles, but is junk.
  **Fix:** strip both.
  **Disposition:** `ConcertServiceTests.cs` no longer exists after the test split; the remaining mid-file BOM
  was removed from `ConcertServiceCreateTests.cs`. A full Concert unit-test source scan reports zero U+FEFF
  matches and the complete Concert unit suite passes 98/98.

- [x] **R20 — critical — posting a concert returns 500. Proven by test, not predicted.**
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
  **Disposition:** the post command and pre-commit handler now resolve the same scoped
  `IConcertPrivilegedRepository`, so the handler observes the tracked aggregate after `Post` and before commit.
  Both `ConcertPostingLifecycleTests` pass, including the outbox-drain and VenueHire payee paths; the full
  Concert integration suite also passes 83/83.

- [x] **R21 — high — settled — adding a filter to the grant *entity* may have emptied three Conversations
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
  **Disposition:** the historical filtered participant queries are gone. Current reads first establish caller
  visibility through the filtered repository, then expand all principal read grants and tenant displays through
  `IConversationPrivilegedRepository`. The exact conversation and preview participant regressions pass 2/2 on
  PostgreSQL; the full Conversations integration suite passes 17/17 on the same current graph.

- [x] **R22 — medium — the permission catalog grants Staff a permission it can never exercise.**
  Every principal grant is issued tenant-wide (`membershipId: null` at `ConcertEntity.cs:98`,
  `ApplicationEntity.cs:65`, `InvoiceEntity.cs:81`, `ThreadEntity.cs:43`), and the `AssignedResources` arm
  requires `grant.MembershipId == ActiveMembershipId`. The only member-grant issuer is
  `ConcertEntity.AssignMember` (Summary + Operations). So Staff/Door/Sound read nothing anywhere except a
  concert explicitly assigned to them — yet `PermissionCatalog.cs:78-83` gives Staff `MessagesRead` and
  `MessagesSend`, which no conversation grant can ever satisfy.
  **Fix:** either issue member-assignment grants on the conversation alongside the concert assignment, or
  remove the message permissions from the assigned-audience roles until P1 has a path that makes them usable.
  Whichever way, the catalog should not assert a capability the predicate forbids.
  **Disposition:** conversations now expose explicit assign/remove-member commands and `ConversationEntity`
  issues live member-specific Read and SendMessages grants only within the principal tenant. Staff retains the
  corresponding catalog permissions, so the assigned-resource predicate can authorize both operations. The
  focused aggregate tests pass 4/4, and the API regression proves assigned Staff read/send access, stale-removal
  conflict, grant preservation and final removal. N8 retains its remaining cross-tenant and rejoin coverage.

- [x] **R23 — low — the settlement succeeded/failed processors now disagree on the same condition.**
  `SettlementPaymentProcessor.cs:48-53` throws for an outcome naming an unknown concert;
  `SettlementPaymentFailedProcessor.cs:38-43` logs and records the inbox receipt for the identical condition.
  Same event family, opposite poison policy: one dead-letters and needs an operator, the other consumes
  silently. The throw does reach durable retry (Service Bus abandons with backoff and dead-letters at
  `MaxDeliveryCount`; the outbox dispatcher records a bounded failure), and no receipt is written on that path
  — both verified.
  **Fix:** pick one policy for an unresolvable target across both processors, and distinguish "concert does not
  exist" from "operation mismatch" in the message so a dead-lettered entry is actionable.
  **Disposition:** both processors now log and throw before recording an inbox receipt when the concert is
  missing or its current settlement operation does not match. The exception messages distinguish those two
  cases, so both event outcomes follow the same durable retry/dead-letter policy. The current-graph settlement
  outcome regressions pass 2/2; the full Concert integration suite passes 83/83.

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

- [x] **N2 — MEDIUM — native/security — concurrent first-tenant creation can escape as an unhandled unique-key failure.**
  `api/src/Modules/Tenant/Concertable.B2B.Tenant.Infrastructure/Repositories/TenantRepository.cs:39-54`
  selects by `CreatedByUserId FOR UPDATE`, but PostgreSQL locks no row when no tenant exists. Two first-create
  requests can therefore both pass `TenantService.cs:114-127`; the unique index at
  `TenantEntityConfiguration.cs:19` rejects one insert without translating it to the declared
  `AlreadyOwnsTenant` outcome.
  **Fix:** serialize on a stable per-user/advisory lock or classify the exact unique violation after rollback;
  add a deterministic two-request race proving one creation and stable typed/HTTP outcomes for both calls.
  **Disposition:** tenant creation now takes a transaction-scoped PostgreSQL advisory lock derived from the
  authenticated user before checking ownership. The deterministic HTTP race holds that exact lock until both
  requests are waiting, then proves one 201, one typed 409, one tenant and one founding membership.

- [x] **N3 — MEDIUM — native — concurrent verification reviews can both succeed and publish conflicting decisions.**
  `api/src/Modules/Tenant/Concertable.B2B.Tenant.Infrastructure/Services/VerificationService.cs:118-125`
  performs an unlocked pending-state read, transition, and save, while
  `TenantVerificationEntityConfiguration.cs:12-17` configures no concurrency token. Approve and reject can
  both observe `Pending`, both return success and notify, and the last update wins.
  **Fix:** lock the verification row for review or use optimistic concurrency and translate the loser to
  `VerificationReviewError.NotPending`; add an approve-versus-reject integration race asserting one durable
  decision and one notification.
  **Disposition:** approve and reject now run their pending-state check, transition and save under a command
  transaction after locking the verification row. Notifications remain post-commit. A server-observed HTTP race
  proves one 204, one typed 409, one durable decision and one notification.

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

- [x] **N5 — LOW — native — Application read paths drop request cancellation before database and cross-module I/O.**
  `api/src/Modules/Application/Concertable.B2B.Application.Infrastructure/Services/ApplicationService.cs:75-160`
  exposes summary, proposal, opportunity, and dashboard reads without a cancellation token and calls
  repositories/modules without one.
  **Fix:** thread `CancellationToken ct = default` through interface, implementation and controller actions,
  pass it to every supporting I/O call, and cover propagation on a representative endpoint.
  **Disposition:** all seven Application read endpoints now thread request cancellation through service,
  repository, application mapping and HTTP response mapping, including Artist, Opportunity and Booking module
  calls. A focused service test proves the exact token reaches the opportunity module, repository and mapper.

- [x] **N6 — MEDIUM — security — concurrent first invoice-number allocation is not serialized.**
  `api/src/Modules/Concert/Concertable.B2B.Concert.Infrastructure/Repositories/InvoiceSequenceRepository.cs:20-28`
  selects the supplier sequence `FOR UPDATE`, but PostgreSQL locks no row when the sequence does not exist;
  `InvoiceIssuer.cs:53-58` then creates it. Two first invoices for one supplier can both allocate `000001`,
  with one later failing a constraint and aborting settlement processing.
  **Fix:** serialize on a stable supplier/advisory key or pre-provision the sequence, or classify and retry the
  exact creation race; add a two-concert first-invoice concurrency test proving unique monotonic numbers.
  **Disposition:** invoice allocation now takes a transaction-scoped PostgreSQL advisory lock derived from the
  supplier tenant before reading or creating its sequence. A server-observed two-concert race holds that exact
  lock until both settlement transactions are waiting, then proves successful unique monotonic allocation.
  Its cleanup releases the control lock before bounded cancellation and aggregate observation, races each child
  action against waiter proof, and preserves the original action exception and stack.

- [x] **N7 — MEDIUM — native/security — concurrent first conversation creation can violate request-id replay semantics.**
  `api/src/Modules/Conversations/Concertable.B2B.Conversations.Infrastructure/Repositories/ConversationRepository.cs:49-67`
  locks an absent receipt key, which PostgreSQL cannot serialize. Two same-actor/same-request calls can both
  create before the unique receipt constraint rejects one; `ConversationService.cs:102-128` has no duplicate
  classification/replay path, so the loser receives a database error.
  **Fix:** serialize on a stable actor/request key or classify the exact duplicate after rollback, re-read the
  winning receipt, and return replay/`RequestConflict`; add a genuinely concurrent integration race.
  **Disposition:** conversation creation now takes a transaction-scoped PostgreSQL advisory lock derived from
  the creator tenant, creating membership and request id before reading the receipt. A server-observed HTTP race
  holds that exact composite key until both requests are waiting, then proves both return the winning conversation.
  The exact race passes 1/1 and the full Conversations integration suite passes 19/19.

- [x] **N8 — LOW — test-impact/security — assigned-member messaging authority lacks integration coverage.**
  `ConversationEntity.cs:43-74` and `ConversationService.cs:264-345` add security-sensitive assignment and
  removal behavior, but only aggregate unit coverage exists.
  **Fix:** add API coverage for assigned Staff read/send access, unassigned denial, removal, cross-tenant
  rejection, stale access version, and membership removal/rejoin not inheriting the old grant.
  **Disposition:** the existing Staff lifecycle regression now also proves unassigned read/send denial,
  cross-tenant assignment rejection without an access-version change, denial after assignment removal, and a
  real remove/invite/accept rejoin whose new membership id cannot inherit the old grant. It retains assigned
  read/send coverage and omitted, stale and current-version removal assertions. The exact regression passes 1/1
  and the full Conversations integration suite passes 19/19.

- [x] **N9 — MEDIUM — seeding/composition — integration setup still inserts B2B users directly.**
  `api/src/Modules/User/Concertable.B2B.User.Infrastructure/Extensions/ServiceCollectionExtensions.cs:62-65`
  registers `UserTestSeeder`, which inserts `SeedState.Users` directly at `UserTestSeeder.cs:23-29` even
  though production creates users only through `CredentialRegisteredEvent`. This can keep integration startup
  green while credential registration or same-flow admin provisioning is broken.
  **Fix:** provision test identity through the real registration handler, preserve the resulting rows across
  Respawn, and assert registration produces both the B2B user and eligible admin profile.
  **Disposition:** the User test seeder now sends every baseline identity through the production
  `CredentialRegisteredHandler`; Respawn preserves the resulting `user.Users` rows between resets. The eligible
  admin integration flow explicitly proves registration created the B2B user before login grants its invited
  admin profile. The exact flow passes 1/1, Admin integration passes 8/8, User integration passes 14/14 and the
  affected graph builds with no warnings or errors.

- [x] **N10 — MEDIUM — E2E — the admin concert-id query uses unquoted PostgreSQL identifiers.**
  `tests/E2ETests/Concertable.B2B.E2ETests.Server/E2EAdminExtensions.cs:176-181` queries
  `SELECT Id FROM concert.Concerts WHERE ApplicationId = ...`, which PostgreSQL folds to lowercase and cannot
  resolve against the quoted PascalCase schema used by the migration and adjacent queries.
  **Fix:** quote schema objects and columns consistently and add an authenticated E2E-admin endpoint smoke
  assertion against seeded data.
  **Disposition:** the concert-id lookup now quotes the `Concerts` table and its `Id` and `ApplicationId`
  columns consistently with the PostgreSQL migration. A focused authenticated endpoint test seeds those exact
  PascalCase identifiers in real PostgreSQL and proves the endpoint returns the matching concert. The exact
  regression passes 1/1, the full E2E-admin integration suite passes 8/8 and the affected graph builds with no
  warnings or errors.

- [x] **N11 — MEDIUM — frontend — losing the final membership leaves a stale active tenant persisted.**
  `app/shared/src/features/tenant/hooks/useTenant.ts:17-19` skips reconciliation when memberships becomes
  empty, although `useTenantStore.ts:28-42` would clear an invalid selection. The stale tenant remains in
  Zustand/local storage until another route resolution or logout.
  **Fix:** reconcile or explicitly clear on the empty-membership transition and test one-membership-to-zero
  behavior across store and persistence.
  **Disposition:** `useTenant` now resolves the tenant session for every membership-set change, including the
  transition to an empty set. The tenant-session regression starts with one membership, persists its selected
  tenant, removes the final membership and proves both the Zustand selection and persisted selection are cleared.
  A hook-level regression exercises that same one-to-zero rerender through `useTenant`, proving the store,
  request session and persisted selection clear exactly once. The shared package passes 39/39 tests and builds.

- [x] **N12 — MEDIUM — frontend contract — membership controls render an unsupported role.**
  `app/shared/src/features/tenant/constants.ts:10` exports `restrictedParticipant`,
  `app/web/shared/src/features/tenant/constants.ts:10` labels it, and `MembersRoster.tsx:68` renders it even
  though the backend `TenantRole` enum has no such member. An authorized user can submit a role the API cannot
  deserialize.
  **Fix:** remove the stale role from the frontend catalog or implement it end-to-end, and prove every rendered
  role is backend-supported.
  **Disposition:** The shared tenant role tuple and web label record now expose exactly the six backend
  `TenantRole` values, so membership controls can no longer submit the unsupported role. Both frontend packages
  pass their test suites and builds.

- [x] **N13 — MEDIUM — mobile permissions — Operations is exposed without `operations.view`.**
  `app/mobile/src/navigation/BusinessNavigator.tsx:33-41` registers the Operations tab for every tenant member
  even though the navigator already receives the active permission set; the Messages tab is correctly gated.
  **Fix:** register Operations only when `permissions.has("operations.view")` and test tab absence/presence.
  **Disposition:** `BusinessNavigator` now registers Operations only through the typed `operations.view`
  predicate. The mobile unit regression proves the tab predicate is false without the permission and true with
  it; the mobile suite and type-check pass.

- [x] **N14 — MEDIUM — mobile authentication — unauthenticated sessions mount account-owned navigation.**
  `app/mobile/src/navigation/RootNavigator.tsx:148-153` sends `user === undefined` to `ArtistTabs`, which
  unconditionally mounts My Artist, Messages, and Profile stacks at `ArtistTabs.tsx:35-59`.
  **Fix:** use a dedicated public/auth navigator or gate every account-owned tab on authentication, and test
  that private routes are absent for an unauthenticated session.
  **Disposition:** Unauthenticated sessions now mount a dedicated public tab navigator containing only Home,
  Search and the shared sign-in/account screen. The public route catalog excludes My Artist, Messages and the
  private profile stack; the mobile regression proves public/authenticated root selection and the exact public
  route set.

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

## Review pass — 2026-09-20 — incremental

**Candidate base:** `a412723de9060b65e5c715f3350d4ead634f032b`
**Candidate head:** `e173633404b52ce1ff08738388b969d7f1c48652`
**Candidate branch:** `Refactor/PartyFoundationLegacyBindings`
**Candidate scope:** `all`
**Candidate path-set:** `sha256:7775b989fb15ea53a682aabe7dd463158e88fc4090de3fd7f4826f48841a0e33` `(9 paths)`
**Candidate bundle:** `C:\Users\TommySeery\source\repos\Concertable\b2b\.git\agent-workflow\runs\party-foundation-remediation-20260920\review\5bcc570928f28759aca9358ece16275fa29e8be21a8592d5f42faf39f951b5d0`
**Candidate bundle identity:** `sha256:e3e1c682a2562b68bc96cc7b0a485d97bf90691c978ed6b555bbfac074c04f42`
**Work-order path:** `reviews/Refactor-PartyFoundationLegacyBindings.md`
**Work-order mode:** `append`
**Pass judgment:** `changes-requested`

### Findings

The native/general lens approved the deterministic duplicate-key recovery, configuration roster, replacement
InitialCreate, mapping tests, and post-recovery database-health proof. The security/data-migration lens approved
the production and schema changes and accepted one test-isolation finding.

- [x] **N24 — MEDIUM — test-isolation — receipt-barrier failure can leave detached HTTP requests.**
  If waiter observation or advisory unlock fails after the concurrent HTTP task starts, the fixture drops its
  trigger without cancelling or awaiting the requests. A late request can mutate after the failed test or fixture
  reset, and a cleanup exception can replace the primary failure.
  **Fix:** give the barrier ownership of a cancellation token, release or close its lock session, bounded-await
  every started request, run DDL cleanup from a fresh connection, and preserve the original failure.
  **Disposition:** the barrier now owns and threads cancellation into both HTTP requests, captures the primary
  failure with its stack, releases the advisory lock or closes its session, bounded-drains the started race, and
  drops the trigger/function through a fresh cleanup connection before rethrowing the original failure. The exact
  server-observed race passes twice and the full access class passes 7/7.

## Review pass — 2026-09-20 — incremental

**Candidate base:** `e173633404b52ce1ff08738388b969d7f1c48652`
**Candidate head:** `317604893224387a1975f2b0177a648380eecdca`
**Candidate branch:** `Refactor/PartyFoundationLegacyBindings`
**Candidate scope:** `all`
**Candidate path-set:** `sha256:913e66c4b0c195825b9cae12159cb4c67a9f7f0eb64955855059dc9a93198f20` `(3 paths)`
**Candidate bundle:** `C:\Users\TommySeery\source\repos\Concertable\b2b\.git\agent-workflow\runs\party-foundation-remediation-20260920\review\4cfd49045453117b7fb8275f350c1b4f3c42d395199a6be416ffdd7483284f27`
**Candidate bundle identity:** `sha256:00177e03fb81e91189a13504aa4bc174a0085476f122e698c0f6faec7b329f83`
**Work-order path:** `reviews/Refactor-PartyFoundationLegacyBindings.md`
**Work-order mode:** `append`
**Pass judgment:** `changes-requested`

### Findings

The security/concurrency lens approved N24. The native/general lens confirmed the success path and accepted one
remaining failure-cleanup issue.

- [x] **N25 — MEDIUM — test-isolation — cancellation and DDL teardown are not bounded end to end.**
  Synchronous unguarded cancellation can block or throw from a callback and replace the captured primary failure.
  If the aggregate HTTP task then exceeds its bounded wait, lock-taking DROP DDL has no helper-owned timeout and
  can extend cleanup indefinitely behind the surviving backend.
  **Fix:** guard and bound cancellation while preserving the first failure, bound PostgreSQL teardown, and prove
  through a forced failure path that the original exception and stack win and the database objects are removed.
  **Disposition:** cancellation now runs asynchronously behind a five-second bound and any callback failure is
  secondary to the captured primary. Teardown uses a ten-second token plus PostgreSQL `lock_timeout` on its fresh
  connection. A forced server-observed failure registers a throwing cancellation callback and proves the original
  exception instance/stack survives while the trigger and function are absent. Both exact regressions pass 2/2 and
  the full access class passes 8/8.

## Review pass — 2026-09-20 — incremental

**Candidate base:** `317604893224387a1975f2b0177a648380eecdca`
**Candidate head:** `b8fe9c6f70b9652cc0359d433b40565bb7687bd6`
**Candidate branch:** `Refactor/PartyFoundationLegacyBindings`
**Candidate scope:** `all`
**Candidate path-set:** `sha256:913e66c4b0c195825b9cae12159cb4c67a9f7f0eb64955855059dc9a93198f20` `(3 paths)`
**Candidate bundle:** `C:\Users\TommySeery\source\repos\Concertable\b2b\.git\agent-workflow\runs\party-foundation-remediation-20260920\review\37b8b0a069d8ea28acf66074c131bd755c0046efe846b0efc7998c4c755d3381`
**Candidate bundle identity:** `sha256:2c6fc941a4d0b065e7628330eb2c6cded4b68a1174fa1f5e46de7436c2755b5f`
**Work-order path:** `reviews/Refactor-PartyFoundationLegacyBindings.md`
**Work-order mode:** `append`
**Pass judgment:** `approved`

### Findings

Both native/general and security/concurrency lenses approved N25. Cancellation, aggregate request observation,
advisory-lock release, bounded DDL teardown, exception precedence, and database-object removal are all verified.
No actionable findings.

## Review pass — 2026-09-20 — incremental

**Candidate base:** `b8fe9c6f70b9652cc0359d433b40565bb7687bd6`
**Candidate head:** `909a1ae1e9f2d3d82f2641fc8a5e632310834338`
**Candidate branch:** `Refactor/PartyFoundationLegacyBindings`
**Candidate scope:** `all`
**Candidate path-set:** `sha256:2c39c3025aec2a4f2aadb0c140fca8b8ddbe06ef328ca4bfaa1adf8f70c68721` `(3 paths)`
**Candidate bundle:** `C:\Users\TommySeery\source\repos\Concertable\b2b\.git\agent-workflow\runs\party-foundation-remediation-20260920\review\34dd25016fe447a1be69cf996ebe311f0aaf36e3fe6e0fc83038d257cfbd6d3f`
**Candidate bundle identity:** `sha256:4b3df7b5f2504605db97421b12a1c0f87f64000979b8181f0adbdeed152ed3a2`
**Work-order path:** `reviews/Refactor-PartyFoundationLegacyBindings.md`
**Work-order mode:** `append`
**Pass judgment:** `approved`

### Findings

Both native/general and security lenses approved R8. Signed big-endian length framing makes the payload
sequence unambiguous, null uses a reserved frame, formatting is culture-invariant, and equivalent UTC/local
instants normalize identically. The collision and normalization regressions and affected integration suites
all pass. No actionable findings.

## Review pass — 2026-09-20 — incremental

**Candidate base:** `909a1ae1e9f2d3d82f2641fc8a5e632310834338`
**Candidate head:** `39265a9d8cb89f091666cca9776e0a20c6a4d924`
**Candidate branch:** `Refactor/PartyFoundationLegacyBindings`
**Candidate scope:** `all`
**Candidate path-set:** `sha256:62c49ac442ae133f6a5a04b2f79222809e4cb4d05d016d8580ed1ce4d6cb546b` `(1 path)`
**Candidate bundle:** `C:\Users\TommySeery\source\repos\Concertable\b2b\.git\agent-workflow\runs\party-foundation-remediation-20260920\review\6d2bf307d8991c3d97eda404e3ec865f3d028cc2e6b3a46d68d7739d2244f45f`
**Candidate bundle identity:** `sha256:84179ef2fa2a2de9c22e72d6f821f75245e1c6c74c1511ba6c3d53f1d95b5fdf`
**Work-order path:** `reviews/Refactor-PartyFoundationLegacyBindings.md`
**Work-order mode:** `append`
**Pass judgment:** `changes-requested`

### Findings

The security lens approved the shared privileged-context and transaction claims. The native lens found that
revoke, assign, and remove still loaded the unfiltered ACL before `CanShareAsync`; actor facts and a generic
permission did not establish resource authority. R1 remained open until those three paths adopted the same
identity-lock, authority-check, full-ACL-load order already used by share and duplicate recovery.

## Review pass — 2026-09-20 — incremental

**Candidate base:** `39265a9d8cb89f091666cca9776e0a20c6a4d924`
**Candidate head:** `86d78b7605ae1da1bfd5af18b7022a6ef5410cd2`
**Candidate branch:** `Refactor/PartyFoundationLegacyBindings`
**Candidate scope:** `all`
**Candidate path-set:** `sha256:bd071b517b929e260dfed3316e4b07b3e528d481e7fe8daf4e227a031d47c0d5` `(3 paths)`
**Candidate bundle:** `C:\Users\TommySeery\source\repos\Concertable\b2b\.git\agent-workflow\runs\party-foundation-remediation-20260920\review\6c60bb4b57ccb1311d99f98c3bf9082346b0c7af35364b487c3611c8163569a6`
**Candidate bundle identity:** `sha256:41f571854c130977ae17f7dcf69aa361e0b619caa818e52a5ba26fa8b6b0acc0`
**Work-order path:** `reviews/Refactor-PartyFoundationLegacyBindings.md`
**Work-order mode:** `append`
**Pass judgment:** `changes-requested`

### Findings

Both lenses approved the production R1 ordering, shared privileged transaction/context, and error semantics.
They independently found that the new HTTP regression only asserted 403 responses and would also pass before
the repair, when the ACL was loaded before denial. R1 remained open until repository-spy coverage could prove
the unfiltered ACL load is never called when resource authority is absent.

## Review pass — 2026-09-20 — incremental

**Candidate base:** `86d78b7605ae1da1bfd5af18b7022a6ef5410cd2`
**Candidate head:** `89952b7e987e7bf2e41f0b0a551ecfe4f682656e`
**Candidate branch:** `Refactor/PartyFoundationLegacyBindings`
**Candidate scope:** `all`
**Candidate path-set:** `sha256:315a1dd97bcfe381ce36d956caf7fe002b0108e16bfeaa0d88af7791a3e85154` `(2 paths)`
**Candidate bundle:** `C:\Users\TommySeery\source\repos\Concertable\b2b\.git\agent-workflow\runs\party-foundation-remediation-20260920\review\1a278489e1e358e55788e28de249e0e703318cc04fce4f9a6a5bbd89592ce649`
**Candidate bundle identity:** `sha256:db39d40e2165bc5e56b2bd721506eb9fc1555f8e2b3a2d29508364db7cea8e79`
**Work-order path:** `reviews/Refactor-PartyFoundationLegacyBindings.md`
**Work-order mode:** `append`
**Pass judgment:** `approved`

### Findings

Both native/general and security lenses approved the R1 repository-spy coverage. The tests execute the real
command and unit-of-work delegates, satisfy every preliminary fact and permission check, force resource
authority denial, and prove the full privileged ACL load is never invoked. No actionable findings.

## Review pass — 2026-09-20 — incremental

**Candidate base:** `89952b7e987e7bf2e41f0b0a551ecfe4f682656e`
**Candidate head:** `6ef6538236a29b69f17ed3ab8647b91866813d0c`
**Candidate branch:** `Refactor/PartyFoundationLegacyBindings`
**Candidate scope:** `all`
**Candidate path-set:** `sha256:9182404cf0a0b1ee6465f3f32f3e728c07a165c97398c9f721f8a1e515d59eeb` `(2 paths)`
**Candidate bundle:** `C:\Users\TommySeery\source\repos\Concertable\b2b\.git\agent-workflow\runs\party-foundation-remediation-20260920\review\47209a6e088712180e92c68e47b1d5396141641e3954b91304fb30bd242b44a2`
**Candidate bundle identity:** `sha256:f5761701c72178e9a8feecef808f39c59a4ed361b18a7e4a0b77ee0dd0ec877b`
**Work-order path:** `reviews/Refactor-PartyFoundationLegacyBindings.md`
**Work-order mode:** `append`
**Pass judgment:** `changes-requested`

### Findings

The native lens approved the isolated missing-grant branch coverage. The security lens found that
`RequestConflict` says the caller reused an idempotency key for a different payload, so returning it for a
matching receipt with corrupt or missing server outcome state was false attribution. R9 remained open until
matching inconsistent outcomes became invariant failures while genuine payload mismatches retained 409.

## Review pass — 2026-09-20 — incremental

**Candidate base:** `6ef6538236a29b69f17ed3ab8647b91866813d0c`
**Candidate head:** `b40ec4a9e5db42ce6d02b98bcf5d2b13cde44da8`
**Candidate branch:** `Refactor/PartyFoundationLegacyBindings`
**Candidate scope:** `all`
**Candidate path-set:** `sha256:da503956f14fa49c1b7b2ec16aa4b97a5cbc634b5f7379fe584cf9e7ec42ce01` `(3 paths)`
**Candidate bundle:** `C:\Users\TommySeery\source\repos\Concertable\b2b\.git\agent-workflow\runs\party-foundation-remediation-20260920\review\6cbc35b4ee22473a09adf29288fefde03c5800e2ce3cb78743f74ee8d8354b10`
**Candidate bundle identity:** `sha256:f69d71ff87885985b6b111706b52e07407c99de28b318d8fba05f13829e701fc`
**Work-order path:** `reviews/Refactor-PartyFoundationLegacyBindings.md`
**Work-order mode:** `append`
**Pass judgment:** `changes-requested`

### Findings

Both lenses approved the corrected production replay semantics. The security lens found one remaining test gap:
the focused suite covered matching replay, payload mismatch, missing receipt, and missing recorded grant, but not
the newly introduced malformed-outcome invariant. R9 remained open until that branch had direct diagnostic and
exception-type coverage.

## Review pass — 2026-09-20 — incremental

**Candidate base:** `b40ec4a9e5db42ce6d02b98bcf5d2b13cde44da8`
**Candidate head:** `a6e72b6f0864c35a895cc4f3337fe46bc5a9235e`
**Candidate branch:** `Refactor/PartyFoundationLegacyBindings`
**Candidate scope:** `all`
**Candidate path-set:** `sha256:9182404cf0a0b1ee6465f3f32f3e728c07a165c97398c9f721f8a1e515d59eeb` `(2 paths)`
**Candidate bundle:** `C:\Users\TommySeery\source\repos\Concertable\b2b\.git\agent-workflow\runs\party-foundation-remediation-20260920\review\20254b4a2fc6b0d7e8b77193e3fa816f5795e89b4949fdb33a89fe4160dba22b`
**Candidate bundle identity:** `sha256:f4973ccb21b1f5d8d8a54b197d7a82a85b2c0e360f818d3e73e086911be10cc8`
**Work-order path:** `reviews/Refactor-PartyFoundationLegacyBindings.md`
**Work-order mode:** `append`
**Pass judgment:** `approved`

### Findings

Both native/general and security lenses approved the malformed-outcome regression and final R9 evidence. The
test cannot exit through payload mismatch or missing-grant classification and verifies the invariant exception,
receipt identity, and specific diagnostic. No actionable findings.

## Review pass — 2026-09-20 — incremental

**Candidate base:** `a6e72b6f0864c35a895cc4f3337fe46bc5a9235e`
**Candidate head:** `f4a953ddd6765a0a68358f8d11dde1f9a6ef348f`
**Candidate branch:** `Refactor/PartyFoundationLegacyBindings`
**Candidate scope:** `all`
**Candidate path-set:** `sha256:bd071b517b929e260dfed3316e4b07b3e528d481e7fe8daf4e227a031d47c0d5` `(3 paths)`
**Candidate patch:** `sha256:a602a3880b28e0193f69c8769d7544ec511212867ec567146f6ed70b6386fb34`
**Candidate bundle:** `C:\Users\TommySeery\source\repos\Concertable\b2b\.git\agent-workflow\runs\party-foundation-remediation-20260920\review\51ede098bcebdaa0359456b7f385f03415261767e451e74653d410c3f37c493b`
**Candidate bundle identity:** `sha256:645dad3a7ac4eb8a1f042d4f179c80d0fb2c0f4953cc52c63e551c56c43ec8d3`
**Work-order path:** `reviews/Refactor-PartyFoundationLegacyBindings.md`
**Work-order mode:** `append`
**Pass judgment:** `approved`

### Findings

Both native/general and security lenses approved R10. Unauthorized callers now receive the concert authority
decision before any caller-controlled recipient lookup, durable replay remains classified from persisted state,
and the focused regression proves the ordering through a nonexistent recipient. No actionable findings.

## Review pass — 2026-09-20 — incremental

**Candidate base:** `f4a953ddd6765a0a68358f8d11dde1f9a6ef348f`
**Candidate head:** `634f71548f44bdb4ab35606480bda39400b0b61c`
**Candidate branch:** `Refactor/PartyFoundationLegacyBindings`
**Candidate scope:** `all`
**Candidate path-set:** `sha256:b05d9e974785926d2746912da6ac6e9483625464bed8b1f4ff9ac60304631571` `(4 paths)`
**Candidate patch:** `sha256:48287503a468d831610ed67e868b4fa79352eefd99b59cc416f4163c4d97de2c`
**Candidate bundle:** `C:\Users\TommySeery\source\repos\Concertable\b2b\.git\agent-workflow\runs\party-foundation-remediation-20260920\review\d33be697896f45cc54ed7eb5928f19f51c409ff8526461d283a1113be4a2105f`
**Candidate bundle identity:** `sha256:747067838b9472d9846deb31d3975be24ccba100f498cc616ab5652250085e2b`
**Work-order path:** `reviews/Refactor-PartyFoundationLegacyBindings.md`
**Work-order mode:** `append`
**Pass judgment:** `changes-requested`

### Findings

The security lens approved R11's authenticated parsing boundary, handler specificity and fixed safe response.
The native lens approved the production mapping and accepted one response-contract test gap.

- [x] **N26 — MEDIUM — test-contract — tenant-header regressions assert only the HTTP status.**
  `ActiveTenantResolutionTests.cs:93-110` detects the prior 500 and handler-order regressions, but an empty 400
  or the wrong Problem Details media type, status, title or detail remains green.
  **Fix:** deserialize both responses as `ProblemDetails` and assert `application/problem+json`, status 400,
  title `Bad Request`, and the fixed safe `X-Tenant-Id` detail.
  **Disposition:** both authenticated requests now assert the response media type and deserialize the complete
  fixed Problem Details contract. The exact regressions pass 2/2 and the Tenant integration project passes
  92/92 after a warning-free rebuild.

## Review pass — 2026-09-20 — incremental

**Candidate base:** `634f71548f44bdb4ab35606480bda39400b0b61c`
**Candidate head:** `f7f1950b543be692809546aa65bad09ac9eb0f79`
**Candidate branch:** `Refactor/PartyFoundationLegacyBindings`
**Candidate scope:** `all`
**Candidate path-set:** `sha256:d87f0b7d18d38251810b79f4b65e1f42d57e9908b8ebe863e0e401fb9b63731a` `(2 paths)`
**Candidate patch:** `sha256:9c315603b80c0b35109ff999e7f6283e9f4ee3c5a72dc795b3c7f7075bfcc650`
**Candidate bundle:** `C:\Users\TommySeery\source\repos\Concertable\b2b\.git\agent-workflow\runs\party-foundation-remediation-20260920\review\037b43042c42ae3d5c423cd377df097029a177b82f37825c363d4fcc52a78756`
**Candidate bundle identity:** `sha256:462f594d0cbc64ac9e58f03aec94e8f9ade6da37298e3d6f6807ec572e24af15`
**Work-order path:** `reviews/Refactor-PartyFoundationLegacyBindings.md`
**Work-order mode:** `append`
**Pass judgment:** `approved`

### Findings

Both native/general and security lenses approved N26. The authenticated malformed and duplicate-header
requests exercise the production handler and assert the complete fixed, non-reflective Problem Details
contract. No actionable findings.

## Review pass — 2026-09-20 — incremental

**Candidate base:** `f7f1950b543be692809546aa65bad09ac9eb0f79`
**Candidate head:** `8ec01a563fc45d9b04b1549ee5e75d35b454bfaa`
**Candidate branch:** `Refactor/PartyFoundationLegacyBindings`
**Candidate scope:** `all`
**Candidate path-set:** `sha256:eb28997054c4bb932ba4f20299393b410e9d5601859dd86aaeeb36fc0d592a0d` `(4 paths)`
**Candidate patch:** `sha256:f578440ebcb9a781d9b913c41d19e4d25e75a333ce946620cef84f063debe6ca`
**Candidate bundle:** `C:\Users\TommySeery\source\repos\Concertable\b2b\.git\agent-workflow\runs\party-foundation-remediation-20260920\review\fe4382e67a726ed116dc75a388cb1b56a7498600841b0bf9a603a6fb5faf8418`
**Candidate bundle identity:** `sha256:180abcc6295b34b145be137226dc8d989d63fc6d2ade9ff32550086e7f254c0e`
**Work-order path:** `reviews/Refactor-PartyFoundationLegacyBindings.md`
**Work-order mode:** `append`
**Pass judgment:** `changes-requested`

### Findings

Both lenses confirmed the new tenant-and-membership predicate was query-correct and cancellation-safe, but
found that R10 had already removed the capability's last business consumer.

- [x] **N27 — MEDIUM — simplification/security — R12 optimizes a dead cross-module API.**
  `ITenantModule.IsCurrentMembershipAsync` and `ITenantService.IsCurrentMembershipAsync` are referenced only
  by their forwarding implementations. The new repository query therefore preserves an unused arbitrary
  tenant/membership existence capability without a behavior-level consumer.
  **Fix:** delete the unused module/service contracts, forwarders and repository query while retaining the
  tenant-wide list used by real member-management behavior.
  **Disposition:** the complete dead chain is deleted and a whole-source search has no remaining
  `IsCurrentMembershipAsync` or `ExistsByTenantIdAndIdAsync` reference.

## Review pass — 2026-09-20 — incremental

**Candidate base:** `8ec01a563fc45d9b04b1549ee5e75d35b454bfaa`
**Candidate head:** `9188aed877e23c1776eb9be92e7c610b4edd2984`
**Candidate branch:** `Refactor/PartyFoundationLegacyBindings`
**Candidate scope:** `all`
**Candidate path-set:** `sha256:2b841dbb1c384b18fdd386b494c721d3f3c354be5db1d60a18b9269d658bd284` `(7 paths)`
**Candidate patch:** `sha256:ddd9b7f504625f2a8ee6dec4d734130553250be11439962f0f8587ac63dbd3d3`
**Candidate bundle:** `C:\Users\TommySeery\source\repos\Concertable\b2b\.git\agent-workflow\runs\party-foundation-remediation-20260920\review\678ea644707353a00f48e8ea18227442d73295451658c426fa9433bf1a61ddcc`
**Candidate bundle identity:** `sha256:b8ab2675f8ed0fb2971009005679a9e153859c8a558107139501486ee5f7cb6b`
**Work-order path:** `reviews/Refactor-PartyFoundationLegacyBindings.md`
**Work-order mode:** `append`
**Pass judgment:** `approved`

### Findings

The native/general lens approved N27. The security/API-contract dispatch timed out after one bounded follow-up;
the parent fallback verified the same frozen deletion reduces the arbitrary existence surface, leaves no
references and retains all four live tenant-wide member-list consumers. No actionable findings.

## Review pass — 2026-09-20 — incremental

**Candidate base:** `9188aed877e23c1776eb9be92e7c610b4edd2984`
**Candidate head:** `2adb9b942fb1263905387299b6dfd2f23ae146cf`
**Candidate branch:** `Refactor/PartyFoundationLegacyBindings`
**Candidate scope:** `all`
**Candidate path-set:** `sha256:744d14cea5eb9e9cd0670efdfdbd7dce1e06f763a6ec4e1ebcf4027582379622` `(4 paths)`
**Candidate patch:** `sha256:f425ed9bca97f0fa701ff87c18bbfd661f8cd22faf9c06eddc5b427c84001c3b`
**Candidate bundle:** `C:\Users\TommySeery\source\repos\Concertable\b2b\.git\agent-workflow\runs\party-foundation-remediation-20260920\review\f86600437f3c179fea3f52332d6903b53a1a48fbea965f48efa7e6febd5c1afc`
**Candidate bundle identity:** `sha256:b83f03061ae74594e7636a2aa4a105dfd2d23f68edbe47f7d58ccff27eaf1008`
**Work-order path:** `reviews/Refactor-PartyFoundationLegacyBindings.md`
**Work-order mode:** `append`
**Pass judgment:** `approved`

### Findings

The native/general and security/data lenses approved R13. The renamed contract now accurately states its
stage-only behavior, the sole caller and implementation agree, the old name is absent, and supplier-tenant
locking, allocation and caller-owned commit behavior are unchanged. No actionable findings.

## Review pass — 2026-09-20 — incremental

**Candidate base:** `2adb9b942fb1263905387299b6dfd2f23ae146cf`
**Candidate head:** `9fb6c0827539d43482ad59c21ffa6c6c7f0c24ff`
**Candidate branch:** `Refactor/PartyFoundationLegacyBindings`
**Candidate scope:** `all`
**Candidate path-set:** `sha256:62c49ac442ae133f6a5a04b2f79222809e4cb4d05d016d8580ed1ce4d6cb546b` `(1 path)`
**Candidate patch:** `sha256:0ec2a3037c98b48dc6a534e8166a40e9c34e1f93c62a6b9fe632c79081650faf`
**Candidate bundle:** `C:\Users\TommySeery\source\repos\Concertable\b2b\.git\agent-workflow\runs\party-foundation-remediation-20260920\review\aa96b0bf8bbe354e32fe17e26d3fa140ba8faa919f40c492ce7a1785ff940dc5`
**Candidate bundle identity:** `sha256:2c3c81f903be291e4020ad7ec392d1ca2acd80b1ea38ed003648905db5c4c3f7`
**Work-order path:** `reviews/Refactor-PartyFoundationLegacyBindings.md`
**Work-order mode:** `append`
**Pass judgment:** `approved`

### Findings

The native/general and security/data lenses approved the R14 reconciliation. Both repository abstractions are
registered, injected and used for every invoice existence, sequence-lock and staging operation, with no direct
`ConcertDbContext` dependency in `InvoiceIssuer`. No actionable findings.

## Review pass — 2026-09-20 — incremental

**Candidate base:** `9fb6c0827539d43482ad59c21ffa6c6c7f0c24ff`
**Candidate head:** `ae5e6717e899e57fefad6a4d60bb3d35d0720655`
**Candidate branch:** `Refactor/PartyFoundationLegacyBindings`
**Candidate scope:** `all`
**Candidate path-set:** `sha256:62c49ac442ae133f6a5a04b2f79222809e4cb4d05d016d8580ed1ce4d6cb546b` `(1 path)`
**Candidate patch:** `sha256:4150140f426819ed1eca327e97a8bbba12c7b837c87b9e8a0e17576b8f632c15`
**Candidate bundle:** `C:\Users\TommySeery\source\repos\Concertable\b2b\.git\agent-workflow\runs\party-foundation-remediation-20260920\review\231b2eb1eecb84793eedf1c0eb0f98d90e48e1e6f5e5f8f53f7abe4356d35b94`
**Candidate bundle identity:** `sha256:6b8154750d519ff43b44058bcf3596ba8a22b05006995e1f3debb4fcee8e428a`
**Work-order path:** `reviews/Refactor-PartyFoundationLegacyBindings.md`
**Work-order mode:** `append`
**Pass judgment:** `approved`

### Findings

The native/general and security/authority lenses approved the R15 reconciliation. No `IsHost` reference remains,
single-owner filtering is exact active-tenant equality, and Concert actions retain explicit party ownership
checks. No actionable findings.

## Review pass — 2026-09-20 — incremental

**Candidate base:** `ae5e6717e899e57fefad6a4d60bb3d35d0720655`
**Candidate head:** `1860be515d4cd69eb57754163d087cceb82ac4fa`
**Candidate branch:** `Refactor/PartyFoundationLegacyBindings`
**Candidate scope:** `all`
**Candidate path-set:** `sha256:44ba80be46546d1e2732aa7ef5329db0d051bc1358dce8f3316dd674ecb795c7` `(3 paths)`
**Candidate patch:** `sha256:a2814ed4fccda6023e3719b783c18f5376df0194143d8f2ace25628a062faa4f`
**Candidate bundle:** `C:\Users\TommySeery\source\repos\Concertable\b2b\.git\agent-workflow\runs\party-foundation-remediation-20260920\review\e6d73ba8cbf6ed7b014d903e819b28327f179612c3ef25719eef80075b60060d`
**Candidate bundle identity:** `sha256:61f04685052ece40ba2f4fc5cc591a4e78ec1c3f0b5d28e64f35a1b50c561784`
**Work-order path:** `reviews/Refactor-PartyFoundationLegacyBindings.md`
**Work-order mode:** `append`
**Pass judgment:** `approved`

### Findings

The native/general and security/authority lenses approved R16. The removed argument was tautological at the
sole caller; target membership remains resolved under the actor tenant, null/cross-tenant targets remain
fail-closed, and the aggregate still requires the actor tenant to be a concert principal. No actionable findings.

## Review pass — 2026-09-20 — incremental

**Candidate base:** `1860be515d4cd69eb57754163d087cceb82ac4fa`
**Candidate head:** `89f4a7810dd3ef21fe6a920e0704d1ff19aa412a`
**Candidate branch:** `Refactor/PartyFoundationLegacyBindings`
**Candidate scope:** `all`
**Candidate path-set:** `sha256:dd85d7cbe5392f1585a4ca7ad5fe09cb5854afa3fc122cb2fe38980fef086731` `(6 paths)`
**Candidate patch:** `sha256:062ca4d55c400e9e11a373ada23abe0ad13f57ec099576b5cbc516cda402bb31`
**Candidate bundle:** `C:\Users\TommySeery\source\repos\Concertable\b2b\.git\agent-workflow\runs\party-foundation-remediation-20260920\review\0632c80ac20b3fc17dafae24921ba43891375b4d750b9854c74f0eff5b2755f3`
**Candidate bundle identity:** `sha256:521223c27fa4fae16914c86484c582e7a89416121dcc940bdc0824b306e4dad7`
**Work-order path:** `reviews/Refactor-PartyFoundationLegacyBindings.md`
**Work-order mode:** `append`
**Pass judgment:** `approved`

### Findings

The native/general and security/data lenses approved R17. The shared expression preserves every live-authority,
tenant, membership-incarnation, revocation, validity, scope and audience predicate; concrete context-member
audiences remain dynamic and the provider-translatable composition contains no `Invoke`. No actionable findings.

## Review pass — 2026-09-21 — incremental

**Candidate base:** `89f4a7810dd3ef21fe6a920e0704d1ff19aa412a`
**Candidate head:** `04659a2d11f60d615406d443ad24a8f05647735c`
**Candidate branch:** `Refactor/PartyFoundationLegacyBindings`
**Candidate scope:** `all`
**Candidate path-set:** `sha256:62c49ac442ae133f6a5a04b2f79222809e4cb4d05d016d8580ed1ce4d6cb546b` `(1 path)`
**Candidate patch:** `sha256:0ea717072c2653ebdbe0e4f66b7e9e07941cf2d32420c541a2bd76acf2b30a28`
**Candidate bundle:** `C:\Users\TommySeery\source\repos\Concertable\b2b\.git\agent-workflow\runs\party-foundation-remediation-20260920\review\315dfdc004e0cd0324fb5cd76de70bc0e56fa86b4e9b95f7b7dc5dcb722d0747`
**Candidate bundle identity:** `sha256:e07e82259a88bbaec83ada2289b845e06010f65e02663ef58ffafae0ffd57894`
**Work-order path:** `reviews/Refactor-PartyFoundationLegacyBindings.md`
**Work-order mode:** `append`
**Pass judgment:** `approved`

### Findings

The native/general and security/docs lenses approved R18. Active source and operative guidance consistently use
`ResourceScopedDbContext`; the old name remains only in explicitly historical plan/review records. The security
lens runner could not recompute bundle hashes, but found no actionable issue in the supplied immutable evidence.

## Review pass — 2026-09-21 — incremental

**Candidate base:** `04659a2d11f60d615406d443ad24a8f05647735c`
**Candidate head:** `bd9e45558b67b088fdd5f7bfe0eb9e223817e422`
**Candidate branch:** `Refactor/PartyFoundationLegacyBindings`
**Candidate scope:** `all`
**Candidate path-set:** `sha256:e739d9eaa3bb818235cad013bdac1ff5170256dd4f04a02b6a74c7c10dd4b9fa` `(2 paths)`
**Candidate patch:** `sha256:78f9e57e08f2e823bd0de41ab19a75be40d9ece6d6527df29e41b199b71ccaa7`
**Candidate bundle:** `C:\Users\TommySeery\source\repos\Concertable\b2b\.git\agent-workflow\runs\party-foundation-remediation-20260920\review\c5bcccc76614d2dfff2e12a274dcab530d672e62f66d69336f625df7c7e6d583`
**Candidate bundle identity:** `sha256:8061f3f3ffe9f360c535a3591312243636e9fb11604577f8262efbad657f95d4`
**Work-order path:** `reviews/Refactor-PartyFoundationLegacyBindings.md`
**Work-order mode:** `append`
**Pass judgment:** `approved`

### Findings

The native/general and security/text lenses approved R19. The only source hunk removes the remaining mid-file
U+FEFF without semantic change; the Concert unit source scan is clean and its full suite passes. No actionable
findings.

## Review pass — 2026-09-21 — incremental

**Candidate base:** `bd9e45558b67b088fdd5f7bfe0eb9e223817e422`
**Candidate head:** `48ded735908dd6eb68f1051c60c613be9c788ecd`
**Candidate branch:** `Refactor/PartyFoundationLegacyBindings`
**Candidate scope:** `all`
**Candidate path-set:** `sha256:62c49ac442ae133f6a5a04b2f79222809e4cb4d05d016d8580ed1ce4d6cb546b` `(1 path)`
**Candidate patch:** `sha256:3a2806f6f0c27336e55a9d1cc9efe4d3686dad82c930de405619a88ae05fe923`
**Candidate bundle:** `C:\Users\TommySeery\source\repos\Concertable\b2b\.git\agent-workflow\runs\party-foundation-remediation-20260920\review\502a304ad3b451e2da0236f67eabedb847222e502f4a613f12d4575484403d3b`
**Candidate bundle identity:** `sha256:283705578d67e820364f85f459f2b09247553a89ede1f2a15676d22529b31008`
**Work-order path:** `reviews/Refactor-PartyFoundationLegacyBindings.md`
**Work-order mode:** `append`
**Pass judgment:** `approved`

### Findings

The native/general and security/data lenses approved R20. Posting and both pre-commit handlers share the same
scoped privileged repository and tracked aggregate, so EF identity resolution exposes post-mutation state. The
exact lifecycle tests pass 2/2 and the full Concert integration suite passes 83/83. No actionable findings.

## Review pass — 2026-09-21 — incremental

**Candidate base:** `48ded735908dd6eb68f1051c60c613be9c788ecd`
**Candidate head:** `e8f6f8cf9ff7fdab045d3b1319c2980842de773a`
**Candidate branch:** `Refactor/PartyFoundationLegacyBindings`
**Candidate scope:** `all`
**Candidate path-set:** `sha256:62c49ac442ae133f6a5a04b2f79222809e4cb4d05d016d8580ed1ce4d6cb546b` `(1 path)`
**Candidate patch:** `sha256:869bb12c5906790183963baa226a7a413265661bc1500881a5f075c36113fcc0`
**Candidate bundle:** `C:\Users\TommySeery\source\repos\Concertable\b2b\.git\agent-workflow\runs\party-foundation-remediation-20260920\review\06ffb96590173d9f0e954b9e02c6298cab430e99e4be7ece06671ac8924f4154`
**Candidate bundle identity:** `sha256:8f9af9db63854bb768f846012a22ced28f22243d923f880e489fa44e0075c1cd`
**Work-order path:** `reviews/Refactor-PartyFoundationLegacyBindings.md`
**Work-order mode:** `append`
**Pass judgment:** `approved`

### Findings

The native/general and security/authorization lenses approved R21. Filtered caller visibility precedes
privileged full-ACL participant expansion, historical filtered counterparty queries are absent, the exact
participant regressions pass 2/2 and the full Conversations suite passes 17/17. No actionable findings.

## Review pass — 2026-09-21 — incremental

**Candidate base:** `e8f6f8cf9ff7fdab045d3b1319c2980842de773a`
**Candidate head:** `99c473f823ad7a19a1fbeefbf98a81a540af4537`
**Candidate branch:** `Refactor/PartyFoundationLegacyBindings`
**Candidate scope:** `all`
**Candidate path-set:** `sha256:62c49ac442ae133f6a5a04b2f79222809e4cb4d05d016d8580ed1ce4d6cb546b` `(1 path)`
**Candidate patch:** `sha256:4935882760bb0d2ad85d5ca7e7c487650eecfb37081fd1afa23af07d26eb66f4`
**Candidate bundle:** `C:\Users\TommySeery\source\repos\Concertable\b2b\.git\agent-workflow\runs\party-foundation-remediation-20260920\review\3543b1f86fd4c3962e9dfd782e67947c550888df4cbc99b7d8f93bd8be734042`
**Candidate bundle identity:** `sha256:e12e381c5689ec5f54ef1da81a4b89523c969c65d5556cf3e68d09254f5e261b`
**Work-order path:** `reviews/Refactor-PartyFoundationLegacyBindings.md`
**Work-order mode:** `append`
**Pass judgment:** `changes-requested`

### Findings

The native/general lens approved R22's member-specific Read and SendMessages grants. The security lens found
that removal omitted the access-version precondition and could revoke a newer re-created assignment.

- [x] **N28 — MEDIUM — security/data — conversation assignment removal is not version-fenced.**
  The DELETE contract passed no expected version, and the locked command skipped its version check when
  removing a member assignment. A stale principal could therefore revoke a grant created after the request's
  read. The acceptance regression also exposed that newly issued GUID-keyed grants were discovered as existing
  entities and sent as concurrency-checked updates rather than inserts.
  **Fix:** require `expectedVersion` on DELETE, compare it after the aggregate lock, return `Superseded` on a
  mismatch, explicitly stage newly issued grants as Added, and prove the Staff assignment lifecycle over HTTP.
  **Disposition:** DELETE now threads a required version through the service and checks it after `FOR UPDATE`.
  Assignment returns its issued grants and the privileged repository explicitly adds them, matching the Concert
  aggregate pattern. The exact Staff assignment/stale-removal regression passes 1/1, Conversations unit tests
  pass 38/38 and the full Conversations integration suite passes 18/18.

## Review pass — 2026-09-21 — incremental

**Candidate base:** `99c473f823ad7a19a1fbeefbf98a81a540af4537`
**Candidate head:** `d1cfd1111b96b5fdfa674f4d3d1acdf084ea44e9`
**Candidate branch:** `Refactor/PartyFoundationLegacyBindings`
**Candidate scope:** `all`
**Candidate path-set:** `sha256:b3eeecf2f3e772257e28a9d2b3cf1b0460a0845871df120218472cfa94d37bb3` `(9 paths)`
**Candidate patch:** `sha256:81ba62132c9565ceeccfe719858172294f036d841894c09d73e9dfbb904ca3ea`
**Candidate bundle:** `C:\Users\TommySeery\source\repos\Concertable\b2b\.git\agent-workflow\runs\party-foundation-remediation-20260920\review\7d3c10bacfe2ea8ec8bb63fe720ffdcd6c7111e1c9cfe30e5e83c6fad47df1e0`
**Candidate bundle identity:** `sha256:862e95f2ba4be3445cd9b6e9f71d1ad83ca22a1fa992da211d0ea52df8cc6353`
**Work-order path:** `reviews/Refactor-PartyFoundationLegacyBindings.md`
**Work-order mode:** `append`
**Pass judgment:** `changes-requested`

### Findings

The security/data lens approved N28's stale-removal version fence, explicit grant staging and complete Staff
assignment lifecycle. The native/general lens found that an omitted value-type query parameter could bind as
zero instead of failing validation.

- [x] **N29 — MEDIUM — native/HTTP — conversation assignment removal does not require the version query key.**
  `[FromQuery] long expectedVersion` accepts omission as zero, so the request reaches the locked command with an
  invented concurrency precondition and can produce the wrong terminal or remove a version-zero assignment.
  **Fix:** make the query binding required and prove an authenticated omission returns 400 without changing the
  aggregate version or assigned member's access.
  **Disposition:** DELETE marks `expectedVersion` binding-required. The Staff lifecycle regression now proves an
  omitted key returns 400, leaves `AccessVersion` unchanged and preserves assigned read access before exercising
  the stale and current-version removal paths.

## Review pass — 2026-09-21 — incremental

**Candidate base:** `d1cfd1111b96b5fdfa674f4d3d1acdf084ea44e9`
**Candidate head:** `db0717dff2e8610d282acfce3118bf2776689965`
**Candidate branch:** `Refactor/PartyFoundationLegacyBindings`
**Candidate scope:** `all`
**Candidate path-set:** `sha256:889e8c9db1f8a7f503f549823ef94fb94f7576d83d3dd9fa57790734638f4829` `(3 paths)`
**Candidate patch:** `sha256:8dd9409226f2a327440f6815ad0d8f0af104b68267174805a680194951e92f27`
**Candidate bundle:** `C:\Users\TommySeery\source\repos\Concertable\b2b\.git\agent-workflow\runs\party-foundation-remediation-20260920\review\c4458a38a545bd0d16f0ed15a2667bf6d962b11a2a1ab0880711a36ac3e36c99`
**Candidate bundle identity:** `sha256:5685b9fce2e0559f58d6c7b96acb7179ef5175b53ce3b027c0f0b91611ce30c3`
**Work-order path:** `reviews/Refactor-PartyFoundationLegacyBindings.md`
**Work-order mode:** `append`
**Pass judgment:** `approved`

### Findings

The native/general and security/API lenses approved N29. The binding-required version query fails with an
automatic 400 before service invocation; the regression proves no version or access change and retains the
independent stale/current lifecycle assertions. No actionable findings.

## Review pass — 2026-09-21 — incremental

**Candidate base:** `db0717dff2e8610d282acfce3118bf2776689965`
**Candidate head:** `87ba51eac49e57ce938ad085c6fe55067bd94964`
**Candidate branch:** `Refactor/PartyFoundationLegacyBindings`
**Candidate scope:** `all`
**Candidate path-set:** `sha256:62c49ac442ae133f6a5a04b2f79222809e4cb4d05d016d8580ed1ce4d6cb546b` `(1 path)`
**Candidate patch:** `sha256:07c01ea037e1e1eba4cc6b0e412bc15a1c82186df94cede420803827a2c56d7c`
**Candidate bundle:** `C:\Users\TommySeery\source\repos\Concertable\b2b\.git\agent-workflow\runs\party-foundation-remediation-20260920\review\43c33e24a179052b0e8ebcc81e8833810c172d3688ab57966436f47eb12407c3`
**Candidate bundle identity:** `sha256:09134feb10f110bbee1fcb659d9df5d28e118922b41891a8333b542abb05e4d0`
**Work-order path:** `reviews/Refactor-PartyFoundationLegacyBindings.md`
**Work-order mode:** `append`
**Pass judgment:** `approved`

### Findings

The native/general and security/durability lenses approved R23. Both settlement processors fail before the inbox
receipt on a missing concert or mismatched operation, distinguish those failures, and therefore retain the same
durable retry/dead-letter policy. No actionable findings.

## Review pass — 2026-09-21 — incremental

**Candidate base:** `87ba51eac49e57ce938ad085c6fe55067bd94964`
**Candidate head:** `542fc2720a7999d61d4841634828d5c91c9845b6`
**Candidate branch:** `Refactor/PartyFoundationLegacyBindings`
**Candidate scope:** `all`
**Candidate path-set:** `sha256:f7d0186ed70755a19275997b6c574f3ba44594611ce862f728a528567d2438bd` `(4 paths)`
**Candidate patch:** `sha256:63b2448ac874c909c4ff40695dbad5ed92570250c67147bf1e7f38aeec005383`
**Candidate bundle:** `C:\Users\TommySeery\source\repos\Concertable\b2b\.git\agent-workflow\runs\party-foundation-remediation-20260920\review\0f4065bab7c048ddd4c671d24fcac7889fa8214ae47b1f80d49f2cee134f2465`
**Candidate bundle identity:** `sha256:3f4f38cb410cb2daff1a200fc0dde6ed956d79a27323a09bd12a0164c610bd96`
**Work-order path:** `reviews/Refactor-PartyFoundationLegacyBindings.md`
**Work-order mode:** `append`
**Pass judgment:** `approved`

### Findings

The native/general and security/data lenses approved N2. The stable per-user advisory lock spans the exact
ownership check, tenant and founding-membership inserts, and commit or rollback. The deterministic two-waiter
HTTP race proves one creation, one typed conflict and one persisted aggregate. No actionable findings.

## Review pass — 2026-09-21 — incremental

**Candidate base:** `542fc2720a7999d61d4841634828d5c91c9845b6`
**Candidate head:** `f982792e5ab690b84ec0b08ca0623cbae21451ee`
**Candidate branch:** `Refactor/PartyFoundationLegacyBindings`
**Candidate scope:** `all`
**Candidate path-set:** `sha256:c78598c52b2f71090f702dcce9ca0498d3069c07b2def97a631bfa52e8f4d5a9` `(8 paths)`
**Candidate patch:** `sha256:0ceaa286768588ba84f8922c3c702656d01ac1678e1743a53355a82d291ada38`
**Candidate bundle:** `C:\Users\TommySeery\source\repos\Concertable\b2b\.git\agent-workflow\runs\party-foundation-remediation-20260920\review\2411fd943902e16ead6409c73b0fdcdd082e0a481702def6f37fdebc7b42a24b`
**Candidate bundle identity:** `sha256:707a69add071abe04ad9ea4b9115330afc7639b3f68b89034e40606112f5c301`
**Work-order path:** `reviews/Refactor-PartyFoundationLegacyBindings.md`
**Work-order mode:** `append`
**Pass judgment:** `changes-requested`

### Findings

Both native/general and security/durability lenses approved N3's production row lock, typed loser conflict and
post-commit notification ordering. Both accepted the same test-isolation finding.

- [x] **N30 — LOW — test-isolation — verification race-barrier failure can leave detached HTTP requests.**
  The barrier starts the aggregate HTTP work before its server-wait proof and control commit, but only awaits it
  on the success path. A waiter or commit failure can release the row lock while the requests continue into the
  next fixture reset or fault unobserved, and the barrier failure can obscure the request failure.
  **Fix:** own and thread cancellation into both requests, preserve the primary exception, always release the
  control transaction, then boundedly cancel and observe all started work before rethrowing the primary.
  **Disposition:** the barrier now captures the first failure with its stack, releases or rolls back its control
  transaction, bounds cancellation and aggregate observation, and rethrows the first failure. A forced failure
  after both server-observed waiters proves that exact exception and helper stack survive cleanup.

## Review pass — 2026-09-21 — incremental

**Candidate base:** `f982792e5ab690b84ec0b08ca0623cbae21451ee`
**Candidate head:** `d900d27dd89b9eae79c14a4acbc8991a6b5be29d`
**Candidate branch:** `Refactor/PartyFoundationLegacyBindings`
**Candidate scope:** `all`
**Candidate path-set:** `sha256:a8e3c1400229754869f69d0c263de4fff766434fab6ce206e43cef73eb4fc684` `(3 paths)`
**Candidate patch:** `sha256:560f7ca35ec8924ef6ffa18bb606f82f02be76fe998769e5739a1ad23d5f4c87`
**Candidate bundle:** `C:\Users\TommySeery\source\repos\Concertable\b2b\.git\agent-workflow\runs\party-foundation-remediation-20260920\review\a020d26e1036e76332aa925ddecf9aa38cc340a51c7007bacbb6539f67443c82`
**Candidate bundle identity:** `sha256:0649baf7d974b770eaf880719e3456b609a684e502add5fb070a0294e60ffc1f`
**Work-order path:** `reviews/Refactor-PartyFoundationLegacyBindings.md`
**Work-order mode:** `append`
**Pass judgment:** `approved`

### Findings

The native/general and security/durability lenses approved N30. The barrier owns cancellation for both requests,
resolves the control transaction before bounded cancellation and aggregate observation, preserves the primary
exception and stack, and deterministically exercises that cleanup after both server-observed waiters. No
actionable findings.

## Review pass — 2026-09-21 — incremental

**Candidate base:** `d900d27dd89b9eae79c14a4acbc8991a6b5be29d`
**Candidate head:** `5e5cd0db7b99f6e4dbc351b48e01a7eb1cec4739`
**Candidate branch:** `Refactor/PartyFoundationLegacyBindings`
**Candidate scope:** `all`
**Candidate path-set:** `sha256:b87f95ea36687232e8c18c8cb20d669af886c54fa7ef3920f452c67b9b39ec21` `(8 paths)`
**Candidate patch:** `sha256:f5c78c457d74639fd70ae80f6378315850a188a22b0ad62aa96163fe879d3da5`
**Candidate bundle:** `C:\Users\TommySeery\source\repos\Concertable\b2b\.git\agent-workflow\runs\party-foundation-remediation-20260920\review\ed6f312b0d0dbf51a4f2f915f0013b37388997650ecdf38c0407b9de432df1e4`
**Candidate bundle identity:** `sha256:958b37f3f5427a15efdf1127d62493cb0d03587244aa00058331641cd31a82d6`
**Work-order path:** `reviews/Refactor-PartyFoundationLegacyBindings.md`
**Work-order mode:** `append`
**Pass judgment:** `approved`

### Findings

The native/API-contract and security/durability lenses approved N5. All seven Application read routes propagate
the request token through controller, service, repository and cross-module mapping to the final EF operations.
The exact-token regression is sensitive to token identity. No actionable findings.

## Review pass — 2026-09-21 — incremental

**Candidate base:** `5e5cd0db7b99f6e4dbc351b48e01a7eb1cec4739`
**Candidate head:** `553b867eaffe88a8b529577c2ba9ef6c4b0cb34c`
**Candidate branch:** `Refactor/PartyFoundationLegacyBindings`
**Candidate scope:** `all`
**Candidate path-set:** `sha256:14e373ad6c558d202a5835e1501e09ebe82478c4122a74dac9f1c3fc6ac719ed` `(4 paths)`
**Candidate patch:** `sha256:006525a3b5463d064daba962bcf898eac2cc1a6142ef86a6c7d62cf3b9895e2d`
**Candidate bundle:** `C:\Users\TommySeery\source\repos\Concertable\b2b\.git\agent-workflow\runs\party-foundation-remediation-20260920\review\a017ee6071fcbf5b349a3269deb6707ddb389c065b83c5c9b096ca401bce47d2`
**Candidate bundle identity:** `sha256:feccb0fc9fda6bed9d6c7d12f0d1e4f69a099509f869d46ec684f5d535f51e3a`
**Work-order path:** `reviews/Refactor-PartyFoundationLegacyBindings.md`
**Work-order mode:** `append`
**Pass judgment:** `changes-requested`

### Findings

Both lenses approved N6's production advisory lock and successful two-waiter allocation proof. The
security/durability lens found that an early child failure could be masked by the waiter timeout and that
aggregate success observation was unbounded.

## Review pass — 2026-09-21 — incremental

**Candidate base:** `553b867eaffe88a8b529577c2ba9ef6c4b0cb34c`
**Candidate head:** `c50cccde364c416cf43b7742156c5c013c4bafee`
**Candidate branch:** `Refactor/PartyFoundationLegacyBindings`
**Candidate scope:** `all`
**Candidate path-set:** `sha256:8ed26b7d26cce71a05059d4b259ef9a569025eb8cc645f24a6a83e70da42384b` `(3 paths)`
**Candidate patch:** `sha256:d325a7c2b75ae346d363af87c2c76c9ff7070a66258d8d3ccda2b1877949bc70`
**Candidate bundle:** `C:\Users\TommySeery\source\repos\Concertable\b2b\.git\agent-workflow\runs\party-foundation-remediation-20260920\review\c8588cb06bb1050a637873d0b035d6e020ddbe36c2acb452afead8344bcfe8db`
**Candidate bundle identity:** `sha256:682410af944e8b067279fd3c9a1132f7095d69e3a20bb6c759e7338f480cbe1f`
**Work-order path:** `reviews/Refactor-PartyFoundationLegacyBindings.md`
**Work-order mode:** `append`
**Pass judgment:** `changes-requested`

### Findings

The security/durability lens approved the bounded cleanup and first-failure preservation. The native/general
lens found that the forced-failure regression threw only after a real settlement completed, so it did not prove
the child-before-waiter failure branch.

## Review pass — 2026-09-21 — incremental

**Candidate base:** `c50cccde364c416cf43b7742156c5c013c4bafee`
**Candidate head:** `a198277bcf557f4e419c20e0d55354d8ee4ccc49`
**Candidate branch:** `Refactor/PartyFoundationLegacyBindings`
**Candidate scope:** `all`
**Candidate path-set:** `sha256:a3a379b7d9ee3db9e737bb1f94a34b4d15ec540aaee70b509bfc4c380ecbf43a` `(2 paths)`
**Candidate patch:** `sha256:cc7ca99380e3a6b1344b4495d2b995dd7b089a5fe93450446f739b308297b763`
**Candidate bundle:** `C:\Users\TommySeery\source\repos\Concertable\b2b\.git\agent-workflow\runs\party-foundation-remediation-20260920\review\992aea735aff0190b24b721595b9ab4f40f85c1526c0627f928bdd8102626550`
**Candidate bundle identity:** `sha256:42744cb2c1e5d74957d591deccf1562bbf60aac7cb252bc33ebf17250206f1d9`
**Work-order path:** `reviews/Refactor-PartyFoundationLegacyBindings.md`
**Work-order mode:** `append`
**Pass judgment:** `approved`

### Findings

The native/general and security/durability lenses approved N6's final test repair. The regression forces one
child to throw before any request while its sibling starts normally, bounds prompt exception propagation,
preserves exception identity and stack, proves sibling cancellation and observation, and independently proves
the advisory lock is released. No actionable findings.

## Review pass — 2026-09-21 — incremental

**Candidate base:** `a198277bcf557f4e419c20e0d55354d8ee4ccc49`
**Candidate head:** `dca9a2828ae040afa80597695b2cd7b61ba25fac`
**Candidate branch:** `Refactor/PartyFoundationLegacyBindings`
**Candidate scope:** `all`
**Candidate path-set:** `sha256:45c6c14e311402bcf502763f809f9ca71afe03ea0878ea81e814c6f95b8cfbe8` `(4 paths)`
**Candidate patch:** `sha256:6e59837df0350db36366e4d89885bb931a1486ef014ce73fa23ada0c50c35071`
**Candidate bundle:** `C:\Users\TommySeery\source\repos\Concertable\b2b\.git\agent-workflow\runs\party-foundation-remediation-20260920\review\d587f74f383faa5e2719cd28ff7148e47b909d692a3ffe5414344b8fdfcece22`
**Candidate bundle identity:** `sha256:9c89c33ffff6d7c1b35289b071fb63159685eb67976536aec195e38e251ef37f`
**Work-order path:** `reviews/Refactor-PartyFoundationLegacyBindings.md`
**Work-order mode:** `append`
**Pass judgment:** `approved`

### Findings

The native/general and security/durability lenses approved N7. The dedicated transaction advisory key uses the
resolved creator tenant, membership and request id before the absent receipt read and remains held through the
winner's commit or rollback. The server-observed race proves two actual same-key waiters return the same durable
conversation, with bounded cleanup and aggregate observation. No actionable findings.

## Review pass — 2026-09-21 — incremental

**Candidate base:** `dca9a2828ae040afa80597695b2cd7b61ba25fac`
**Candidate head:** `81ced39caf109386290a3cb6756702286c47b2d5`
**Candidate branch:** `Refactor/PartyFoundationLegacyBindings`
**Candidate scope:** `all`
**Candidate path-set:** `sha256:705c3991dc69b4dcd9290421d025d1890e868131bab7bdb7e13dbdd277a31ab2` `(2 paths)`
**Candidate patch:** `sha256:dd9ea89bb77e98b33488becf6e0e5959900fc6b57d8025355772115ad42928fc`
**Candidate bundle:** `C:\Users\TommySeery\source\repos\Concertable\b2b\.git\agent-workflow\runs\party-foundation-remediation-20260920\review\dd00e0d57d60c9c4513f32bd4fbd72c96a829ac57ba0bd5bfc6cdafeca541f9e`
**Candidate bundle identity:** `sha256:0c18c4c41f0acff7fb59fe81673a4a3f8c41b8ea3623ccaa70ceb82bccaca8ce`
**Work-order path:** `reviews/Refactor-PartyFoundationLegacyBindings.md`
**Work-order mode:** `append`
**Pass judgment:** `approved`

### Findings

The native/general and security lenses approved N8. The real-API lifecycle proves exact member-scoped read/send
authority, cross-tenant and version rejection without mutation, current-version removal, and that a newly
accepted membership cannot inherit the deliberately live grant issued to its removed predecessor. No actionable
findings.

## Review pass — 2026-09-21 — incremental

**Candidate base:** `81ced39caf109386290a3cb6756702286c47b2d5`
**Candidate head:** `913b71df10e85b9f860314e8c308328cfa3ee006`
**Candidate branch:** `Refactor/PartyFoundationLegacyBindings`
**Candidate scope:** `all`
**Candidate path-set:** `sha256:4f64d9c4b3c0b64cd1e384af2f18e9d4af0e1ed8f2377102a9b30f83251fdc37` `(5 paths)`
**Candidate patch:** `sha256:3f8c0400e6be626b7a45d41ca534a42a0e868cde98ff61ecfee8f57db54ee3a3`
**Candidate bundle:** `C:\Users\TommySeery\source\repos\Concertable\b2b\.git\agent-workflow\runs\party-foundation-remediation-20260920\review\27f00bc99e1db54fe8f7c9c54a8d0c0e9810a30c7a9c1d5308a06fd4d3d5f7d3`
**Candidate bundle identity:** `sha256:80c6ea775af56bda7d9ecd232eb950700f2d068cd39c8e0a9842522ebaaf4c56`
**Work-order path:** `reviews/Refactor-PartyFoundationLegacyBindings.md`
**Work-order mode:** `append`
**Pass judgment:** `approved`

### Findings

The native/general and security/durability lenses approved N9. Test identities now traverse the production
registration handler under its real scoped composition. Respawn retains only baseline user rows while each reset
removes transient registrations; the admin flow proves registration creates a plain B2B user before authenticated
login can consume the exact active invitation and grant that same user admin authority. No actionable findings.

## Review pass — 2026-09-21 — incremental

**Candidate base:** `913b71df10e85b9f860314e8c308328cfa3ee006`
**Candidate head:** `a9073c661da738cf8031408dbf9c8b09c413c752`
**Candidate branch:** `Refactor/PartyFoundationLegacyBindings`
**Candidate scope:** `all`
**Candidate path-set:** `sha256:c7603bbac7dd8db7435b9daa12f2efc5cbb24cd589fb840050ddb4ef3fe9f5df` `(3 paths)`
**Candidate patch:** `sha256:7b5e35a398cd6bd00fa04d66b9140f3f77c9ed785a9d1bf50d415010d710e16f`
**Candidate bundle:** `C:\Users\TommySeery\source\repos\Concertable\b2b\.git\agent-workflow\runs\party-foundation-remediation-20260920\review\a6d1cbc6c666f2ef4788d9da2023520aaad0adb117cff4c6eb66fa5088456dd9`
**Candidate bundle identity:** `sha256:15b2f8c151952801608b4de36ab80aeb1b632136d2aa97075df9d51b776f5282`
**Work-order path:** `reviews/Refactor-PartyFoundationLegacyBindings.md`
**Work-order mode:** `append`
**Pass judgment:** `approved`

### Findings

The native/general and security/durability lenses approved N10. The endpoint remains E2E-only and behind the
fixed-time key filter; its Dapper query now uses the exact quoted PostgreSQL schema/table/column identifiers with
a bound application-id parameter. The real-PostgreSQL regression traverses the authenticated endpoint and retains
the missing-key, blank-key and non-E2E denials. No actionable findings.

## Review pass — 2026-09-21 — incremental

**Candidate base:** `c29230da770f36fbc6e1c5538128b92922b73a53`
**Candidate head:** `b569ec38271772035744deebbb3756d25b2eda54`
**Candidate branch:** `Refactor/PartyFoundationLegacyBindings`
**Candidate scope:** `all`
**Candidate path-set:** `sha256:56d42a400157de5ca3b9fcb5fcabd98ca976461c034b9e0dba2c4b1acd75b0be` `(2 paths)`
**Candidate patch:** `sha256:4465a26bd3c28df55c839b8e41c685bd4d98fa09ddae8779b94b9da9d723f0fe`
**Candidate bundle:** `C:\Users\TommySeery\source\repos\Concertable\b2b\.git\agent-workflow\runs\party-foundation-remediation-20260920\review\78fc4006599ab3c1c148a32271e73268a4daaa78f8ea93f976d3427e44f1a60e`
**Candidate bundle identity:** `sha256:27cc23636e1af6addd0ee22779402a5696c7fef9555b3515d6343bc631ca0426`
**Work-order path:** `reviews/Refactor-PartyFoundationLegacyBindings.md`
**Work-order mode:** `append`
**Pass judgment:** `approved`

### Findings

The native/general and security/durability lenses approved N11's hook-level repair. The regression invokes the
actual `useTenant` lifecycle across a one-membership-to-zero rerender with the production store initializer and
real tenant session, then proves store, request-session and persistence clearing with exactly one save and clear.
No actionable findings.

## Review pass — 2026-09-21 — incremental

**Candidate base:** `b569ec38271772035744deebbb3756d25b2eda54`
**Candidate head:** `dab86afc3da06bc3563f3fac89d9ca6db0a2d0a5`
**Candidate branch:** `Refactor/PartyFoundationLegacyBindings`
**Candidate scope:** `all`
**Candidate path-set:** `sha256:29f911ac3ed597ddbdc57c6f36886b0b3b22634c11a8388bb8c9f62d691638f8` `(3 paths)`
**Candidate patch:** `sha256:72e405418e8e311d6467a5f3f208ea00ecf0532f75df24b5fd7567e33e3baa27`
**Candidate bundle:** `C:\Users\TommySeery\source\repos\Concertable\b2b\.git\agent-workflow\runs\party-foundation-remediation-20260920\review\b59d83fc6620323b15d27ef33748e62b415124b30b8131dba9d62b831cea4856`
**Candidate bundle identity:** `sha256:b0830a80cf1f05a9122dd3942021a5b0a72502bc3d59444c9250ae5b0737c898`
**Work-order path:** `reviews/Refactor-PartyFoundationLegacyBindings.md`
**Work-order mode:** `append`
**Pass judgment:** `approved`

### Findings

The native/general and security/durability lenses approved N12. The shared wire tuple and derived role type now
exactly match the backend enum's six values; the exhaustive label map and sole roster option source carry only
those typed values through the role request. The unsupported role is absent from application source. No
actionable findings.

## Review pass — 2026-09-21 — incremental

**Candidate base:** `dab86afc3da06bc3563f3fac89d9ca6db0a2d0a5`
**Candidate head:** `e70393fb8e87fba8f793d5f56f2df1e74f985f2d`
**Candidate branch:** `Refactor/PartyFoundationLegacyBindings`
**Candidate scope:** `all`
**Candidate path-set:** `sha256:a6ed4bd3c1a394758855ec2957ac894e46d92d2c8ca3b322ca2006e984258c88` `(6 paths)`
**Candidate patch:** `sha256:3ba74249b0c37bb97e76d46df23250e1e112a9156af673fc8f855facfb870c80`
**Candidate bundle:** `C:\Users\TommySeery\source\repos\Concertable\b2b\.git\agent-workflow\runs\party-foundation-remediation-20260920\review\1d5f9acdef8d5e27ad6da47a295f96f17d5043ec9b31057fff60a4854c798809`
**Candidate bundle identity:** `sha256:b5b4467cac20258963375a2be2baa76d7f0c7d8af2607d3b4365d669b37dfc64`
**Work-order path:** `reviews/Refactor-PartyFoundationLegacyBindings.md`
**Work-order mode:** `append`
**Pass judgment:** `approved`

### Findings

The native/general and security/durability lenses approved N13. The typed, fail-closed `operations.view`
predicate directly controls registration of the sole Operations route. Active-membership permission changes
rerender the keyed navigation tree, and pure regressions cover both predicate branches. No actionable findings.
