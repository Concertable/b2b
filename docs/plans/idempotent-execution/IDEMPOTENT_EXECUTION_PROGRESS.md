# Operation claims and attempt classification progress

- Plan: `docs/plans/idempotent-execution/IDEMPOTENT_EXECUTION_PLAN.md`
- Roadmap: [`Concertable/docs` launch roadmap](https://github.com/Concertable/docs/blob/main/plans/launch/LAUNCH_ROADMAP.md)
- Roadmap item: `launch/operation-claims-and-attempts`
- Worktree: `.worktrees/Refactor-DealVocabularyAndMapperCollapse`
- Branch: `Refactor/DealVocabularyAndMapperCollapse` — replayed into this repo from the archived
  monorepo branch `refactor/launch_operation-claims-and-attempts` (`513298a1`), never pushed there
- PR: none yet
- Dependency/package gates: **no package gate**. Both phases are B2B-internal by design (plan §4) — the types land
  in `Concertable.B2B.DataAccess.Application`/`.Infrastructure`, which every module consumes by
  `ProjectReference`. No `Concertable.*` published contract changes, so no producer PR and no
  `chore/platform-sync-*`. **Delivery gate:** none remaining — the stack base is gone with the monorepo.
- Last reconciled: 2026-09-14 — ported into the `Concertable/b2b` polyrepo as commits `7d0e3024`
  and `34f5042b`; the original stacked base (#633) no longer applies, the monorepo is archived.

## Current state

**Both phases are implemented. Phase 1 is committed; Phase 2 is committed on top.**

Phase 1 put five claims across three aggregates onto one composed `OperationClaim` mapped as an EF owned
entity type, with `has-pending-model-changes` clean on all three contexts — no migration.

Phase 2 replaced the seven copy-pasted conflict blocks with one four-way `AttemptVerdict` and two
`AttemptAsync` executors. The shape that matters: **which interface you are on decides whether a replay is
possible at all.** `IUnitOfWorkBehavior<TContext>.AttemptAsync` takes no budget and runs once, because its
change tracker belongs to the ambient DI scope; `IUnitOfWorkBoundary<TContext>.AttemptAsync` takes one,
because each attempt gets its own context. So accept cannot accidentally regain an in-process rerun, and
`SettlementService` keeps its two attempts by declaring them instead of nesting `TryExecuteAsync` inside
its own classifier.

`AcceptOnceAsync`, `IScoped<ApplicationWorkflow>`, the concrete `AddScoped<ApplicationWorkflow>()` and the
design-narration comment are gone from source. Accept's third classifier branch now reports
`AcceptApplicationError.Contended` — `application.accept.contended`, 409 — instead of rerunning.

The constraint `where TContext : DbContextBase` was dropped from both executors: neither
`IUnitOfWorkBehavior<TContext>` nor `IUnitOfWorkBoundary<TContext>` constrains its context, so copying the
constraint from the implementations only made the loop harder to test.

## Next Steps

**State: implementable work is complete; delivery-gated.** Both phases are committed on
`Refactor/launch_operation-claims-and-attempts`. Do **not** push to `Refactor/launch_deal-lifecycle-modules-phase2` —
PR #633 is out of draft in the merge queue, and a push there resets its review watermark.

1. Push the branch and open a PR **with `Refactor/launch_deal-lifecycle-modules-phase2` as its base**, so
   it stacks. GitHub retargets it to the default branch when #633 merges.
2. `/review` the branch — this is a cross-cutting change to three lifecycle aggregates and the accept
   error contract, so expect the correctness lens on the concurrency story.
3. Merge only after #633 is terminal.

Not yet run locally, deliberately: the B2B integration and E2E tiers. Exact-head CI owns them, and this
environment cannot build the full solution (see the disk note below).

## Completed work

- 2026-09-06 — design investigation, plan and ledger. No source changed.
- 2026-09-06 — **Phase 1: the operation claim.** `OperationClaim` + `IHasCancellationClaim` +
  `OwnsClaim`/`OwnsRequiredClaim`/`WithCancellationClaim`; `ApplicationEntity`, `BookingEntity` and
  `ConcertEntity` migrated with their three EF configurations; ten consumers migrated
  (`ApplicationWorkflow`, `BookingWorkflow.Matches`, `BookingRepository`, `BookingService` x2, the two
  confirm steps, both outcome processors, `SettlementService`); the `TECH_DEBT.md`
  operation-claim entry **deleted**; `OperationClaimTests` added.
- 2026-09-06 — **Phase 2: attempt classification.** `AttemptVerdict<TOutcome>` +
  `AttemptExtensions.AttemptAsync` on both plumbings; all seven conflict sites migrated
  (`ApplicationService` Withdraw/Reject/Cancel, `ApplicationWorkflow` Accept, `BookingWorkflow` Cancel,
  `ConcertWorkflow` Cancel, `SettlementService` Reserve at budget 2);
  `AcceptApplicationError.Contended` added; `AcceptOnceAsync`, `IScoped<ApplicationWorkflow>` and the
  concrete `AddScoped<ApplicationWorkflow>()` deleted;
  `Accept_WhenPaymentVerificationWinsTheRace_ReportsContendedAndSucceedsOnRetry` updated;
  `AttemptExtensionsTests` added.

## Verification

Phase 1, all local:

- **`has-pending-model-changes`: "No changes" on `ApplicationDbContext`, `BookingDbContext` and
  `ConcertDbContext`.** The authoritative no-migration check, and the one that would have caught a wrong
  column name, a wrong index name or the missing `IsRequired()`.
- Unit suites green after Phase 2: `DataAccess.UnitTests` 24/24 (11 `OperationClaimTests` +
  13 `AttemptExtensionsTests`),
  `Application.UnitTests` 20/20, `Booking.UnitTests` 9/9, `Concert.UnitTests` 105/105.
- Module builds green with 0 warnings: Application, Booking and Concert Infrastructure, plus
  `Concertable.B2B.Web`.

Integration and E2E tiers belong to exact-head CI (no local E2E). Phase 2's gate is plan §6.

## Reviews

No review yet — no implementation exists to review.

## Decisions, discoveries, blockers, and deviations

- **Do not run a full `api/Concertable.slnx` build in this environment.** It fails with `CS0041` /
  `MSB3021` — *"There is not enough space on the disk"*, not a code error — and burns ten minutes doing
  it. C: has ~0.9 GB free, with 34.7 GB in `.worktrees` (`Refactor-launch_deal-lifecycle-modules-phase2`
  16.3, `Chore-TestTierNaming` 10.3, this one 6.8) and 6.5 GB in the NuGet cache. The phase gate is the
  smallest affected project build plus focused tests; exact-head CI owns the full solution and the
  complete matrix. Phase 1 passed that gate before the disk filled.

- **`Claim()` must resume, not re-mint.** `OperationId ?? Claim(Guid.NewGuid())`. The first draft minted
  a fresh id and then hit the rival check, which throws — that breaks `BeginSettlement`'s documented
  reuse and its unit test `BeginSettlement_WhenPreviousAttemptFailed_ReusesTheOperation`. Found by
  probing, before any production code existed.
- **A latent double-refund bug is fixed as a side effect.** Both `BeginCancellation` methods do
  `= Guid.NewGuid()` unconditionally while `BeginSettlement` does `??=`, and
  `CancellationFailed → BeginCancellation` is a legal edge in both state machines. Payment's
  `EscrowService.RefundByReferenceCoreAsync` resumes a `Pending` refund and replays a `Completed` one
  keyed on that id, with a unique index on `PaymentRefundEntity.OperationId` — it is built for reuse, so
  a fresh id on cancel-retry starts a second refund against the same escrow. Name it in the commit
  message; it is a fix, not a refactor.
- **The retry taxonomy is four-way, and the plumbing owns the budget** — not an argument at seven call
  sites. `IUnitOfWorkBehavior` is scope-backed so it can only ever attempt once; `IUnitOfWorkBoundary` is
  factory-backed so it may declare a budget. This is what prevents re-collapsing "transient" into
  "recoverable", which is what produced the deleted accept rerun.
- **`Transient` ships with no production producer, deliberately.** Deadlocks and timeouts are unhandled
  500s today — `IsXConflict` matches only `DbUpdateConcurrencyException`/duplicate-key and no context
  enables `EnableRetryOnFailure`. Giving it a producer needs the unit of work off the DI scope (upstream
  S2). Unit tests cover it; do not "wire it up" by routing a business race into it.
- **`SettlementService.ReserveAsync` is a seventh copy the original brief missed**, and it nests
  `TryExecuteAsync` inside its own classifier. It is correct for it to retry in place — its caller is a
  settlement trigger with no user to re-ask — so it is the case that proves the budget belongs to the
  boundary. Budget 2 reproduces its behaviour exactly.
- **No frontend change.** The B2B SPA does nothing code-specific with these errors: mutations have no
  retry configured at all, `shouldRetry` never retries 409, nothing branches on an error code, and the
  client's `ProblemDetails` type does not even declare `code`. The error *message* is the whole client
  contract, which is why the recoverable branch needs its own case rather than reusing `Superseded`.
- **Only one integration test pins the behaviour being changed** —
  `Accept_WhenPaymentVerificationWinsTheRace_StillConfirmsTheBooking`. `ArmOnce` arms a single conflict,
  so it becomes a 409 followed by a successful re-POST with `ForcedConflicts` still 1.
