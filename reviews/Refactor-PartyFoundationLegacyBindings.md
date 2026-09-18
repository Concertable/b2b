# Code review — Refactor/PartyFoundationLegacyBindings

> **This file is a work order, not a discussion.** If you're handed this file, fix the open `[ ]`
> findings directly and report what changed. Tick each `[x]` as you land it. Pause only for a genuinely
> irreversible or ambiguous finding: record its durable disposition, take the safe path, and keep going.

**Review status:** `complete`
**Reviewed up to commit:** the restarted branch's single documentation commit  `(2026-09-15)`
**Judgment:** `approved`

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
