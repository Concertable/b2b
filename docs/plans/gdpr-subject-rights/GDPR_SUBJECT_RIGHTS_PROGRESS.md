# GDPR Subject Rights — Erasure + Data Export progress

- Plan: `docs/plans/gdpr-subject-rights/GDPR_SUBJECT_RIGHTS_PLAN.md`
- Recovery: `RECOVERY.md` beside this file — read it before acting on anything below.
- Roadmap: `Concertable/docs` `plans/launch/LAUNCH_ROADMAP.md`, item `launch/gdpr-subject-rights`
- Branch: `Feature/MonorepoBacklogPort` (`b2b`). Supersedes the archived monorepo branch
  `Feature/launch_gdpr-subject-rights` and its permanently-unmergeable draft PR
  [#707](https://github.com/Concertable/concertable/pull/707).
- Dependency/package gates: **Pre-merge delivery gate** — solicitor retention-policy / retain-vs-erase
  sign-off (swim-lane A, tracked in `LAUNCH_CHECKLIST.md` Phase 2). This gates **merge, not
  implementation** (see `## Decisions`). Cross-service delivery is multi-PR, producer-first, all additive —
  no published-contract shape change, so no expand/contract gate.
- Last reconciled: 2026-09-14, ported into `b2b` and **unpaused**: the deal-lifecycle carve has landed, and
  the Concert-side reads were rebuilt across the Application/Booking/Concert facades it produced. Solution
  builds clean; Privacy unit suite 22/22 and all 22 architecture guards green.

## Current state

**Phase 1 is built and now lives in `b2b`. The 2026-08-22 pause is lifted** — the
`launch/deal-lifecycle-ownership` carve landed and the Concert-side reads were rebuilt against it
(`RECOVERY.md`). What follows describes the work as built; the per-module reads are now three facades, not
one `IConcertModule` fragment. Step 1 (docs) is done: the
standing `docs/plans/gdpr-subject-rights/GDPR_SUBJECT_RIGHTS.md` compliance doc + the `LEGAL_REQUIREMENTS.md` item-8 flip. The
code (new `Concertable.B2B.Privacy` module + four module-facade extensions + unit/integration suites) is
committed and pushed; `PrivacyDbContext.InitialCreate` is scaffolded.

Naming/design refactors landed on the branch after the first build: the obligation `Gate` → `Checker`
(`ISubjectObligationChecker`); broad-facade export methods carry their subject where it adds information
(`IConcertModule.ExportRecordsAsync`, `IConversationsModule.ExportMessagesAsync`; `IUserModule.ExportAsync`
stays bare — the subject is redundant); the two endpoints are rate-limited (`RateLimitPolicies.Sensitive`).
The **export was re-shaped to a real file download**: `ISubjectExporter.ExportAsync` now returns a
`FileDownload` (composed straight into an indented-JSON file, `Content-Disposition` via the controller's
`File(...)`), the unconsumed `SubjectExportBundle` aggregate DTO was deleted, and the integration test now
deserialises the downloaded file. The **consumption contract is pinned** in the plan (design decision 5) and
the DSAR operator UI is written into `launch/admin-console` as Phase 5 (which also owns the new `GET`
erasure-list endpoint the queue needs). The shared `FileDownload` + `ToFileResult()` consolidation is logged
in `TECH_DEBT.md` (publish-first, deferred).

**Blocker discovered 2026-08-22:** the Concert-side reads — the obligation check (reads `Application` +
self-billing) and the records export (reads `Invoice`/`Contract`/self-billing), plus the `IConcertModule`
fragment additions — are built against the current monolithic Concert module, which
`launch/deal-lifecycle-ownership` (PR #633 chain) is decomposing into Opportunity/Application/Booking/Concert.
That carve decides `Invoice`/`Contract`/self-billing module placement *during* the carve and deletes
`IConcertModule`-as-umbrella, so the Concert fragment must be rebuilt against the new boundaries afterwards.
The in-flight Concert-fragment naming rework (records service/DTO) was reverted to the last buildable commit
rather than finished, since its target module does not exist yet.

The B2B patterns were mapped exhaustively (module facades, the `FinishExecutor`/`ConcertFinishedFunction`/
`ISelfBillingAgreementGate` fail-closed shape, the `LifecycleState`/`FrozenDictionary` state machine, the
`TenantScopedRepository`/alias persistence, the Admin-module scaffolding + `[Authorize(Policy="Admin")]`
authority, `initial-migrations.ps1`). The subject surface + retain-vs-erase table are in the plan; the design
orchestrates erasure/export through each module's own facade only, per `../../../ARCHITECTURE.md`.
Erasure/export/soft-delete/retention machinery is confirmed **absent** across `api/` — a green field.

The roadmap marker was de-duplicated: `` `launch/gdpr-subject-rights` `` was carried on **two** checklist
lines (the §"Build" blocker and the §7 launch-ready gate); the §7 gate line was reworded to reference the
build-blocker (matching the webhook/tenant-verification/admin-console convention), leaving the canonical,
still-unchecked marker on the build-blocker line only. The roadmap line is **not** ticked — the feature has
not shipped.

## Next Steps

The 2026-08-22 blocker is **cleared**. `launch/deal-lifecycle-ownership` (PR #633 chain) has landed:
`ApplicationEntity` lives in the Application module, `ContractEntity` in Booking, and `InvoiceEntity` +
`SelfBillingAgreementEntity` stay in Concert. The Concert fragment was rebuilt against those boundaries
rather than ported — see `RECOVERY.md` for exactly what changed and why.

Remaining before this ships:

1. **Run the integration suite.** `SubjectRightsApiTests` is Testcontainers-gated, was never run on the
   workstation, and has therefore never executed anywhere. Its seeded-state assumptions
   (`ArtistManagerNoArtist` clean, `VenueManager1` obligated) were written against the pre-carve lifecycle
   and must be re-confirmed against the post-carve one.
2. **Re-scaffold `PrivacyDbContext.InitialCreate`.** The migration is carried over verbatim from the
   monorepo (`20260821182404_InitialCreate`); `./initial-migrations.ps1 -Context PrivacyDbContext -Check`
   confirms whether it still matches the model.
3. **The solicitor retention-policy sign-off** still gates merge (unchanged — see `## Decisions`).
4. Phases 2-5 as the plan describes, including the admin-console DSAR UI.

Everything else stands and needs no rework: the `Concertable.B2B.Privacy` module core (erasure
aggregate/state machine/gate, admin route, unit + integration tests), the `SubjectExporter` file-download
shape, the User/Tenant/Conversations fragments, and the plan/ledger/tech-debt edits. **#707 stays a draft —
do not `/review` or merge until the Concert side is rebuilt.**

## Completed work

- Feature plan + this ledger authored; worktree `Feature/launch_gdpr-subject-rights` created off
  `origin/main` (`7f59fe27b`), clean.
- Roadmap `launch/gdpr-subject-rights` marker de-duplicated to a single canonical (unchecked) build-blocker
  line so the plan graph resolves exactly one marker.
- **Step 1 (docs):** `docs/plans/gdpr-subject-rights/GDPR_SUBJECT_RIGHTS.md` compliance doc written; `LEGAL_REQUIREMENTS.md`
  item 8 flipped ABSENT → DESIGNED (link verified).
- **New `Concertable.B2B.Privacy` module** (Domain/Application/Infrastructure/Api + Unit/Integration tests):
  `SubjectErasureRequestEntity` aggregate + `ErasureState`/`ErasureTrigger`/`ErasureStateMachine` (FrozenDictionary,
  fail-closed) + `ErasureTransitionError`; `ISubjectErasureService` (owns the aggregate/repo/UoW), `IErasureGate`,
  `ISubjectExporter`; `PrivacyDbContext : DbContextBase` (unscoped) + config/factory/provider/schema + repo alias;
  migrate-only Dev/Test seeders; `SubjectRightsController` `[Authorize(Policy="Admin")]` at `POST /api/subject-erasure/{id}`
  + `GET /api/subject-export/{id}`. Wired into `.slnx`, `B2BWebHostExtensions` (AddPrivacyApi + AddPrivacyDevSeeder),
  base `ApiFixture` (AddPrivacyTestSeeder), and `initial-migrations.ps1`.
- **Per-module facade additions** (delegating, zero cross-module queries): User `EraseAsync`/`ExportAsync` +
  domain `Anonymise` + `UserExport`; Tenant `SeverMembershipsAsync` (wound-down detection)/`PurgePendingInvitationsAsync`
  via a focused `TenantErasureService` + repo finders; Conversations `SeverAuthoredMessagesAsync`
  (`MessageEntity.SeverAuthor`)/`ScrubParticipantProfilesAsync`/`ExportAsync` via `ConversationsErasureService` +
  a privileged participant-profile repo + `MessageExport`; Concert `HasLiveObligationsAsync` (`ConcertObligationGate`
  over the unfiltered read stance) + `ExportAsync` (`ConcertRecordsExporter` + `ConcertExportMappers`) +
  `ConcertRecordsExport`/`InvoiceExport`/`ContractExport`/`SelfBillingAgreementExport`; `IConcertReadDbContext`
  gained `Applications`/`Invoices`/`Contracts`.
- **Tests:** Privacy unit suite 20/20 green (state machine edges + fail-closed, entity, `ErasureTransitionError`
  definition contract, gate, erasure-service orchestration incl. defer-vs-complete and email-before-erase ordering).
  `SubjectRightsApiTests` integration written (clean→completed+anonymised, obligated→deferred+intact, export scope,
  admin-gate reachability) — validates in the merge queue.

## Verification

- `dotnet build Concertable.B2B/Concertable.B2B.slnx`: **0 errors, 4 warnings** (whole solution incl. Privacy
  module + both new test projects + fixtures).
- Privacy unit suite (`dotnet test …Privacy.UnitTests`): **20/20 passed**.
- `./initial-migrations.ps1`: run in progress at this checkpoint (scaffolds `PrivacyDbContext.InitialCreate`).
- `python .agents/hooks/plan_graph.py --root <worktree>`: pending this checkpoint.
- Integration suite: Testcontainers-gated — not run on the workstation; validates in the merge queue.

## Reviews

None yet — Phase 1 PR not yet opened; route through `/review` once opened.

## Decisions, discoveries, blockers, and deviations

- **A new `Concertable.B2B.Privacy` vertical-slice module owns the erasure orchestration** (the scalable
  answer over bolting GDPR domain logic onto Admin). It holds the `SubjectErasureRequest` aggregate + state
  machine, the fail-closed erasure gate, the export assembler, and the admin-gated controller. Layers:
  `Privacy.Domain` (aggregate + `ErasureState`/`ErasureTrigger` + `ErasureStateMachine`), `Privacy.Application`
  (service/repo/gate/eraser interfaces, `RequestErasureError`, `SubjectExportBundle`, the `ErasureOutcome`
  success-side enum), `Privacy.Infrastructure` (`PrivacyDbContext : DbContextBase` — unscoped, admin-operated,
  subject-`Guid`-keyed; repo alias on `Guid`; gate/eraser/service impls; DI), `Privacy.Api` (controller). No
  `Privacy.Contracts` yet (no cross-module consumer until Phase 2/5 — YAGNI per module-structure).
- **All data mutation is delegated to the four owning modules via new facade members** (zero cross-module
  queries): `IUserModule` ERASE `UserEntity` (new domain `Anonymise()` nulls Address/Location/Avatar +
  tombstones Email) + export fragment; `ITenantModule` SEVER memberships under the last-owner invariant +
  PURGE pending invitations + sole-trader tax handling + export; `IConversationsModule` scrub
  `ParticipantProfile` (re-project to pseudonym) + SEVER `MessageEntity` author link + export;
  `IConcertModule` `HasLiveObligationsAsync(tenantIds)` for the gate (Booked/AwaitingSettlement applications +
  current `SelfBillingAgreementEntity`) + RETAIN-only export fragment (invoices/contracts, untouched).
- **The gate orchestrates:** Privacy → `ITenantModule.GetMembershipsAsync(userId)` → tenantIds →
  `IConcertModule.HasLiveObligationsAsync(tenantIds)`; `false`→proceed, live→`Deferred` (never throws),
  mirroring `FinishExecutor`'s success-side `Deferred…` outcome. Payment obligations join the gate in Phase 4.
- **Admin gate reused, not reinvented:** the controller is `[Authorize(Policy="Admin")]` against the existing
  data-driven `AdminProfiles` policy (owned by the Admin module, present in the Web host + integration
  fixtures); Privacy does not define a second admin concept and does not reference `Admin.Api`.
- **Integration tests use public-facade read-back; the "RETAIN byte-for-byte" assertion is partial.** The
  canonical `SeedState` seeds **no** invoice/contract/self-billing rows (those are created at Accept/Finish,
  never seeded), so there is no financial RETAIN row to assert survives a *completed* erasure. The suite
  proves RETAIN the reachable way — the obligated-subject test asserts a deferred erasure leaves **every** row
  intact — but the stronger "clean subject anonymised *while a financial row it owns survives*" case needs a
  seeded/hand-built `SelfBillingAgreementEntity` (expired, so it is not itself a live obligation). Deferred as
  a Phase-1 integration follow-up rather than shipped as a blind, unrunnable seed (the Testcontainers suite is
  not runnable on this workstation; it validates in the merge queue).
- **Pre-merge delivery gate (does NOT block implementation).** The solicitor sign-off is a delivery gate,
  not a hard blocker — the design is against the *known* HMRC six-year financial retention, so implementation
  proceeds now and every retain-vs-erase call is recorded for the solicitor to confirm. Recorded in the
  four-field form for the delivery gate (the owner is external / swim-lane A, so there is no sibling
  `_PROGRESS.md` to register a reciprocal handoff against, and this is deliberately **not** placed in
  `## Next Steps`):
  - Blocked: merge of any PR that ships the erasure/export capability to production.
  - Blocked by: solicitor retention-policy / retain-vs-erase sign-off — swim-lane A, tracked in
    `LAUNCH_CHECKLIST.md` Phase 2 ("Data retention schedule documented", "DSAR process documented", `[LEGAL]`).
  - Unblock action: solicitor ratifies the retain-vs-erase table + DSAR SLA in the standing
    `docs/plans/gdpr-subject-rights/GDPR_SUBJECT_RIGHTS.md` compliance doc (Phase 1 deliverable).
  - Resume when: the compliance doc's `[LEGAL]` sign-off checklist is confirmed by the solicitor.
- **Subject surface corrections from the code map** (do not re-derive): messaging is the **B2B Conversations
  module**, not a separate service; there is **no `BookingAgreementEntity`** — the signed artifact is
  `ContractEntity` and the aggregate is `BookingEntity`; Customer `ReviewEntity` is keyed by **`Email`** (no
  `UserId`), so its erasure matches on email; the transport is a **custom transactional-outbox bus**
  (`Concertable.Messaging`), not MassTransit; the fan-out to mirror is `CredentialRegisteredEvent`; the
  fail-closed pattern to mirror is `FinishExecutor` + the hourly `ConcertFinishedFunction` sweep.
- **`launch/admin-console` is a soft (UX) dependency, not an implementation blocker.** The capability + a
  reachable admin-gated route are testable without the admin SPA (as admin-provisioning was); the polished
  operator UI is the admin console's tenant when it lands.
- **Cross-service delivery is additive.** New erasure event, new facade members, new Payment gRPC method — no
  published `Concertable.*` contract shape changes, so multi-PR delivery keeps the codebase in sync at every
  phase boundary and needs no expand/contract cycle. If one is discovered, it becomes its own plan.

## Resume prompt

```
cd C:\Users\TommySeery\source\repos\Concertable.worktrees\Feature\launch_gdpr-subject-rights
Read @docs/plans/gdpr-subject-rights/GDPR_SUBJECT_RIGHTS_PLAN.md and @docs/plans/gdpr-subject-rights/GDPR_SUBJECT_RIGHTS_PROGRESS.md and do what its `## Next Steps` says.
```
