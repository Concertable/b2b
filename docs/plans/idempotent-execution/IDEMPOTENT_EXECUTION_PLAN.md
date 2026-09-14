# Operation claims and attempt classification

> **Next steps live in @docs/plans/idempotent-execution/IDEMPOTENT_EXECUTION_PROGRESS.md → `## Next Steps`.**

Delivered as a **stacked branch on top of PR #633**, based at `55fd099d9`. The members it rewrites —
`AcceptanceOperationId`, the module-owned `BookingEntity`/`ConcertEntity` claims, and the six
`TryExecuteAsync` blocks — exist in that shape only on that branch, so the stack's base carries them and
nothing is duplicated. Implementation proceeds now; the stack cannot merge until #633 does, at which point
GitHub retargets it to the default branch.

## 1. The problem

Two unrelated things are copy-pasted across the B2B lifecycle modules, and neither has an abstraction.

**(1) The idempotency claim.** Five long-running operations anchor themselves to a row with an
operation id, in three different shapes, with no shared vocabulary:

| Entity | Field | Shape today |
|---|---|---|
| `ApplicationEntity` | `AcceptanceOperationId` | caller supplies the id; `??=` then throw if it differs |
| `ConcertEntity` | `SettlementOperationId` | entity mints (`??= Guid.NewGuid()`); separate `Ensure` throws two ways |
| `ConcertEntity` | `CancellationOperationId` | entity mints, **unconditionally** (`= Guid.NewGuid()`) |
| `BookingEntity` | `OperationId` | claimed in the constructor, non-nullable, verified inline |
| `BookingEntity` | `CancellationOperationId` | entity mints, **unconditionally** |

The two decisions that vary are **who mints the id** and **claim versus verify**. An operation spanning
entities cannot reason about a claim uniformly. `ApplicationEntity.BeginAcceptance` also assigns before
validating the assignment, and carries a no-argument overload whose only caller is a unit test.

Recorded as the MEDIUM entry in `TECH_DEBT.md`; Phase 1 deletes that entry.

**(2) The execution/retry policy.** This block is copy-pasted six times — `ApplicationService`
Withdraw/Reject/Cancel, `ApplicationWorkflow` Accept, `BookingWorkflow` Cancel, `ConcertWorkflow`
Cancel:

```csharp
unitOfWorkBehavior.TryExecuteAsync(
    () => XCoreAsync(...),
    exception => exception.IsXConflict(id),
    _ => ClassifyXConflictAsync(id, ct),
    ct);
```

`SettlementService.ReserveAsync` is a seventh, on `IUnitOfWorkBoundary`, which additionally **nests**
`TryExecuteAsync` inside its own classifier — the same hand-rolled `attempts < 2` as the deleted
`AcceptOnceAsync`, written as nesting rather than as a second method.

The root-cause analysis is S4 of `~/.claude/plans/Concertable/B2B_SCOPE_AND_TRANSACTION_BOUNDARIES.md`,
with the accept decision settled in `~/.claude/plans/Concertable/ACCEPT_RETRY_INVESTIGATION.md`.

## 2. The classification is four-way

The failure being deleted came from collapsing "transient" into "recoverable" — an in-process rerun of a
whole business operation in a fresh DI scope, to paper over a race. The taxonomy names each case so that
collapse stops being expressible:

| Verdict | Meaning | Action |
|---|---|---|
| `Settled(outcome)` | the conflict achieved the caller's intent | return the outcome |
| `Transient(exception)` | nothing about the world changed — deadlock, timeout, dropped connection | replay while budget remains, else **rethrow** |
| `Recoverable(outcome)` | the world changed; the attempt was valid and could succeed now | replay while budget remains, else **report** the outcome |
| `Unrecoverable(outcome)` | the state has moved on | report, never replay |

`Settled` is not filler: `ApplicationService` Withdraw/Reject/Cancel and `BookingWorkflow` Cancel all
return `new Success()` today when the winner did what the loser wanted. `Transient` and `Recoverable`
differ precisely in what happens when the budget is spent, which is what stops them re-merging.

**The budget is not an argument at seven call sites — the plumbing decides.**

- `IUnitOfWorkBehavior<TContext>` is **scope-backed**: the change tracker belongs to the DI scope, so a
  replay would run against a dirty tracker. Its `AttemptAsync` runs **once, always**; no budget parameter
  exists to get wrong, and `Transient` rethrows immediately — today's behaviour.
- `IUnitOfWorkBoundary<TContext>` is **factory-backed**: each attempt gets its own context, so it may
  declare a budget.

Accept is scope-backed and returns `Recoverable`, so it reports — the decision already made upstream,
now enforced by the type rather than by a comment.

`Transient` ships with **no production producer**, deliberately. Deadlocks and command timeouts are
unhandled 500s today: `IsXConflict` matches only `DbUpdateConcurrencyException`/duplicate-key, and no
context enables `EnableRetryOnFailure`, so `CreateExecutionStrategy()` is non-retrying. Giving it a
producer needs the unit of work off the DI scope (upstream S2). It is covered by unit tests, not by
production code, and it exists so the next person routes a deadlock to it instead of to `Recoverable`.

## 3. Evidence — measured, not assumed

Probed against EF Core 10.0.3 before committing to a mapping. These four facts decide the design:

| Question | Result |
|---|---|
| `ComplexProperty` + `HasIndex("Claim.OperationId")` | **fails** — `InvalidOperationException`; EF 10 cannot index a complex-type member |
| `OwnsOne` + `HasColumnName`/`HasIndex`/`HasFilter`/`HasDatabaseName` | exact column, index name, uniqueness and filter parity, table-shared |
| generic query through `where T : class, IHasCancellationClaim` | translates to identical SQL for two different entities |
| claim-only UPDATE | **byte-identical to today's**, with the owner's concurrency token in the `WHERE` |

The last one was the real risk. Mutating an owned claim leaves the owner entry `Unchanged` and only the
owned entry `Modified`; had the write not carried the owner's token it would have silently broken
optimistic concurrency — the exact defect `cdb79aa49` fixed. Measured:

```
OWNED:  UPDATE "OwnedBookings" SET "CancellationOperationId" = @p0 WHERE "Id" = @p1 AND "State" = @p2
PLAIN:  UPDATE "PlainBookings" SET "CancellationOperationId" = @p0 WHERE "Id" = @p1 AND "State" = @p2
```

EF merges the owned command with the principal's concurrency tokens under table splitting. A stale token
throws `DbUpdateConcurrencyException` with **both** entries present, so the existing row-scoped
predicates (`entry.Entity is ApplicationEntity a && a.Id == applicationId`) keep matching unchanged.

**So the mapping is an owned entity type, not a complex type**, with the index moved inside the `OwnsOne`
builder and `HasDatabaseName` pinning the existing name. An owned entity type is also mutable by design,
which is what lets the claim be the `EventRaiser` shape — a composed collaborator with behaviour — rather
than an immutable value object handing back a new instance on every claim.

## 4. Placement — both halves are B2B-internal, no package gate

`Concertable.Kernel` and `Concertable.DataAccess.*` are **published packages** here
(`<PackageReference … Version="$(ConcertablePlatformVersion)" />`, with no project-reference escape), and
`plans/AGENTS.md` is explicit that a published-contract change needs its own plan and cannot land in one
PR. Putting either half in those packages would force a producer PR plus a platform version-sync PR
before B2B could consume it.

Neither needs to:

- `OperationClaim`, `IHasCancellationClaim` and `AttemptVerdict<TOutcome>` go in
  **`Concertable.B2B.DataAccess.Application`**, a `ProjectReference` from every module, beside
  `IConcurrencyVersioned` and `IVenueArtistTenantScoped`. `IConcurrencyVersioned` is the precedent — a
  wholly generic capability that still lives in B2B's own data-access library rather than Kernel.
- The `AttemptAsync` **extension members** go in `Concertable.B2B.DataAccess.Infrastructure`. Extension
  members are compile-time, so they attach to the published `IUnitOfWorkBehavior<T>` /
  `IUnitOfWorkBoundary<T>` without changing either package.

Both promote to the shared packages the day a second service wants them. Payment carries four
operation-id columns of the same shape (`EscrowEntity.ReleaseOperationId`,
`PaymentRefundEntity.OperationId`, `TransactionEntity.OperationId`, `PaymentSessionEntity.OperationId`)
but is not migrated here, and a future consumer is not a current one — shared code is the intersection,
never the union.

## 5. Phase 1 — the operation claim

Independent of Phase 2 and shippable alone.

**The type** — `Concertable.B2B.DataAccess.Application/OperationClaim.cs`:

```csharp
public sealed class OperationClaim
{
    public Guid? OperationId { get; private set; }
    public bool IsHeldBy(Guid operationId);
    public Guid Claim();          // entity mints -- RESUMES if already held
    public Guid Claim(Guid id);   // caller mints -- validates, THEN assigns
}
```

Both `Claim` overloads are one claim-or-resume path, so "who mints" stops being a shape; `IsHeldBy` is
the verify half. `Claim()` must be `OperationId ?? Claim(Guid.NewGuid())` — minting first and then hitting
the rival check breaks `BeginSettlement`'s documented reuse and its unit test
`BeginSettlement_WhenPreviousAttemptFailed_ReusesTheOperation`.

**The capability interface** — `IHasCancellationClaim { OperationClaim Cancellation { get; } }`, matching
the house `IHasName` / `IHasDateRange` / `ITenantScoped` convention.

**Consumption contract.** Generic infrastructure binds to the interface, not to either entity:
`ClaimedBy<T>(this IQueryable<T> source, Guid operationId) where T : class, IHasCancellationClaim`
returns the entity holding that cancellation claim, translated server-side to
`WHERE [CancellationOperationId] = @p`. Its two consumers are
`Booking/CancellationFinancialOperationOutcomeProcessor` and
`Concert/FinancialOperationOutcomeProcessor`, which each hand-roll
`SingleOrDefaultAsync(v => v.CancellationOperationId == operationId, ct)` today. Both migrate in this
phase; the capability ships with no unmigrated consumer.

**Not** adding `IHasSettlementClaim`/`IHasAcceptanceClaim`: every caller of those holds the concrete
entity (`SettlementService` a `ConcertEntity`, `BookingWorkflow.Matches` a `BookingEntity`), so they
would be interfaces with no consumer. Additive when one appears; the uniform vocabulary does not depend
on them.

**Migration, per entity** — replace the `Guid?` property with a composed claim, move the index inside
`OwnsOne`, pin the column and index names:

- `ApplicationEntity` — `Acceptance`. `BeginAcceptance(Guid)` delegates to `Claim(id)`; the no-argument
  overload is deleted and its unit-test caller passes `Guid.NewGuid()`. `Accept(snapshot)`'s
  `snapshot.OperationId != AcceptanceOperationId` becomes `!Acceptance.IsHeldBy(snapshot.OperationId)`.
- `ConcertEntity` — `Cancellation`, `Settlement`. `EnsureSettlementOperation`'s two throws collapse to
  one `IsHeldBy` check naming both the held and the expected id.
- `BookingEntity` — `Acceptance`, `Cancellation`. `Acceptance` is claimed in the constructor from the
  snapshot and its owned property needs **`IsRequired()`**, because `OperationId` is non-nullable with an
  *unfiltered* unique index. `Matches`'s `booking.OperationId == accepted.OperationId` becomes
  `booking.Acceptance.IsHeldBy(accepted.OperationId)`.

**A latent bug this fixes — name it in the commit message.** Both `BeginCancellation` methods do
`= Guid.NewGuid()` unconditionally while `BeginSettlement` does `??=`, and
`CancellationFailed → BeginCancellation → CancellationPending` is a legal edge in both state machines.
That id is the idempotency key on `RefundEscrowCommand`, and `EscrowService.RefundByReferenceCoreAsync`
**resumes** a `Pending` refund and **replays** a `Completed` one keyed on it, with a unique index on
`PaymentRefundEntity.OperationId` — Payment is built for reuse. A fresh id on cancel-retry starts a
second refund against the same escrow. `Claim()` resuming fixes it.

**Verification gate.** `dotnet ef migrations has-pending-model-changes` reports **no** pending changes for
the Application, Booking and Concert contexts — the authoritative "no migration" check, and the one that
catches a wrong column name, a wrong index name, or the `IsRequired()` omission. Then the three modules'
unit and integration suites, plus new `OperationClaim` unit tests (resume, rival, empty, verify).
`ConcertApiFixture`'s raw `[SettlementOperationId] IS NULL` is unaffected by construction.

## 6. Phase 2 — attempt classification

Independent of Phase 1 and shippable alone.

**The union** — `AttemptVerdict<TOutcome>` as nested abstract records, the house closed-union idiom
(`SettlementPreparation` is the precedent; there is no generic Dunet union anywhere in the repo).

**Consumption contract.** Two `AttemptAsync` extension members, one per plumbing, each lifting the
operation's success into `Settled`, delegating to the existing `TryExecuteAsync` for transaction and
rollback semantics, and running the verdict loop:

- `IUnitOfWorkBehavior<TContext>.AttemptAsync(operation, isConflict, classify, ct)` — **no budget
  parameter**; one attempt, `Transient` rethrows, `Recoverable`/`Unrecoverable`/`Settled` return.
- `IUnitOfWorkBoundary<TContext>.AttemptAsync(attempts, operation, isConflict, classify, ct)` — replays
  `Transient` and `Recoverable` while the budget remains.

`classify` is `Func<DbUpdateException, Task<AttemptVerdict<TOutcome>>>`; the operation itself stays
retry-free and never learns it is being retried. All seven call sites migrate in this phase.

**Call sites.** The six scope-backed classifiers return `AttemptVerdict` instead of the bare outcome:
`Settled` where they return `new Success()` today, `Unrecoverable` where they return `Superseded` /
`AlreadyAccepted`. `SettlementService.ReserveAsync` declares budget 2 with `Recoverable`, reproducing its
nested behaviour exactly and deleting the nesting; it costs one extra classification read on the first
loss, on a rare path — named here rather than hidden.

**Accept.** `ClassifyAcceptConflictAsync`'s third branch — "nothing forbids it, so the loss was to a
payment verification landing mid-flight" — returns
`Recoverable(new AcceptApplicationError.Contended(id))`.

**Deletions.** `AcceptOnceAsync`; `IScoped<ApplicationWorkflow>` with its field and constructor
parameter; the concrete `AddScoped<ApplicationWorkflow>()` plus the self-resolving `IApplicationWorkflow`
factory registration, collapsing to `AddScoped<IApplicationWorkflow, ApplicationWorkflow>()`; the
design-narration comment in `ClassifyAcceptConflictAsync`; that method's now-unused `eSignature`
parameter.

**The error case.** `AcceptApplicationError.Contended(int ApplicationId)`, code
`application.accept.contended`, `ErrorKind.Conflict` → 409, message instructing the venue to accept
again. No new `ErrorKind`, no frontend change — see §7.

**Verification gate.** Application, Booking and Concert unit + integration suites, plus new unit tests
covering all four verdicts and budget exhaustion. One integration test changes:
`Accept_WhenPaymentVerificationWinsTheRace_StillConfirmsTheBooking` becomes
`..._ReportsContendedAndSucceedsOnRetry` — first POST 409 `application.accept.contended`, second POST 204
with the booking confirmed, `ForcedConflicts` still 1 (`ArmOnce` arms a single conflict).

## 7. The open question, answered from the SPA

**Its own error case, not `Superseded`.** The B2B SPA does nothing code-specific with these errors:
mutations have no retry at all (`retry: shouldRetry` is set on `queries` only in
`app/web/shared/src/lib/queryClient.ts`), `shouldRetry` covers 408/429/502/503/504 and never 409, nothing
in `app/` branches on any error code, and the client's `ProblemDetails` type is
`{ title, detail, errors }` — it does not even declare `code`, which the backend does put on the wire via
`ApplicationProblemDetails.CodeExtensionKey`. The only consumer is a `toast.error` rendering `detail`.

So the **message is the entire client contract**, and `Superseded`'s "changed while this acceptance was in
flight" tells the venue to give up on the one case where clicking Accept again succeeds. The Accept button
is re-enabled after a failed mutation, so the retry affordance already exists and no frontend change is
needed.

## 8. Definition of done

- The five claims share one vocabulary; no entity spells a claim its own way.
- No migration — `has-pending-model-changes` clean on all three contexts.
- Cancel-retry reuses its operation id.
- One executor per plumbing; no classifier nests another, and no second entry point named for a retry
  budget.
- `AcceptOnceAsync`, `IScoped<ApplicationWorkflow>` and its registration are gone from source.
- A contended accept reports `application.accept.contended` and succeeds on a re-POST.
- The `TECH_DEBT.md` operation-claim entry is **deleted**, not archived.

## 9. Out of scope

- S1–S3 of the upstream scope/transaction plan. `Transient` has no production producer until its S2.
- S5's `IServiceScopeFactory` ban and the `IScoped<T>` architecture-test allowlist.
- Migrating Payment's four operation-id columns, or promoting either type into `Concertable.Kernel` /
  `Concertable.DataAccess`.
- Any frontend change.
