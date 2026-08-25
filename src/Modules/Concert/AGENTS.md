# Concert module — the realised-event lifecycle

The Concert module owns the **operational Concert lifecycle only** — draft/posting, cancellation,
settlement, and completion of a realised event. It is **not** an umbrella over the booking chain. Since the
lifecycle carve, `Opportunity → Application → Booking → Concert` authority is split across four modules, each
owning its own aggregate state; Concert is the last stage and reaches back into none of the others. A Concert
is created only from Booking's immutable `ConfirmedBooking` handoff and never loads a live Booking or
Application aggregate, never inspects `Application.State`.

The `keyed-strategies` and `state-machines` skills own the two patterns this module leans on. Read them first;
this doc is only the Concertable-specific roster.

## Vocabulary — the two tenants of a booking sit on TWO axes, not one

A booking has two tenants, and the codebase names them on **two independent axes**. They look like
redundant synonyms; they aren't, and you **cannot** collapse them — a *fixed* field can't hold a
*flipping* value, so unifying the words would make the code wrong (the tenancy filter would point at
the wrong tenant half the time). Which axis a word belongs to:

- **IDENTITY (fixed — *who* the tenant is)** → **`venue`** / **`artist`**. A venue is always the venue.
  This is the tenancy/visibility axis: `IVenueArtistTenantScoped`, `VenueTenantId` / `ArtistTenantId`,
  the `venue == me || artist == me` query filter.
- **ROLE (flips per `DealType` — *what* the tenant does)** — resolved from identity, never stored fixed:
  - **money flow** → **`payee`** (receives the settlement) vs the counterparty. See
    `DealPayeeResolver`, whose cohesive per-deal strategy resolves the ticket collector and inverse
    settlement recipient directly.
  - **VAT invoice** → **`supplier`** (made the supply) / **`customer`** (billed). HMRC's legally-required
    words — you can't put "payee" on an invoice. Mapping: `supplier` = settlement payee, `customer` =
    ticket payee.

**`Party`** is the abstract "one side," and is **reserved for the invoice snapshot VO** (`InvoiceParty`:
a side's legal identity frozen at settlement). It is **not** a synonym for `tenant` — don't use the bare
word "party" as generic glue for "a venue/artist tenant" elsewhere.

The flip is the whole point: on `VenueHire` the venue is the supplier/settlement-payee; on every other
deal the artist is. That's why identity and role must stay separate words.

## The lifecycle — module-owned state, Kernel-backed transitions

There is no per-`DealType` state machine and no shared workflow object. Concert owns one configured machine
for its own stage, in `Domain/Lifecycle/`:

- **`State`** — `Draft, Posted, CancellationPending, CancellationFailed, AwaitingSettlement,
  SettlementFailed, Complete, Cancelled`.
- **`Trigger`** — `Post, BeginCancellation, RecordCancellationFailure, Cancel, BeginSettlement,
  RecordSettlementFailure, CompleteSettlement`.
- **`StateMachine`** — `internal sealed class StateMachine : IStateMachine<State, Trigger>` whose sixteen
  legal edges are copied into a `Concertable.Kernel.StateMachine<State, Trigger>` frozen table (the
  `state-machines` skill owns the shared algorithm). It stores no entity state; a rejected edge returns
  `TransitionError<State, Trigger>`.

`ConcertEntity` holds `State` with a private setter and one `private static readonly StateMachine`. Every
lifecycle mutation funnels through the aggregate's private `Transition(Trigger)` helper, which assigns
`State = next` **only** from the success value, then mutates operation-specific data and raises domain
events; a rejected transition leaves state, auxiliary facts, and events untouched. Callers invoke the
semantic operations (`Post`, `BeginCancellation`, `Cancel`, `BeginSettlement`, `RecordSettlementFailure`,
`CompleteSettlement`) — never a public generic `Transition`. `LifecycleStateOwnershipTests` in the
architecture suite mechanically fails any `State` assignment outside that private path.

Operation errors are operation-owned closed unions (`PostConcertError`, `CancelConcertError`,
`FinishConcertError`), each carrying `InvalidTransition(TransitionError<State, Trigger>)` for a rejected edge
and its own additional expected cases. There is no shared error base or `IError` widening.

## The pieces

- **Creation** — `IConcertService.CreateAsync(ConfirmedBooking)` owns uniform draft creation from the
  immutable Booking handoff. It is the same projection-lookup/genre-intersection/persist/notify/email path
  for every `DealType`; the immutable terms cases supply different data to `ConcertEntity.CreateDraft`, they
  do not select different creation behaviour. Creation is **not** an executor and selects no keyed step.
  Post-commit notification is staged through the outbox and delivered only after the shared confirmation
  transaction commits.

- **Executors** (`Application/Executors` interface, `Infrastructure/Services/Executors` impl) — Concert has
  exactly **two**, one per named lifecycle operation: `ICancelExecutor.CancelAsync(concertId)` →
  `UnitResult<CancelConcertError>`, and `ICompleteExecutor.CompleteAsync(concertId)` →
  `Result<SettlementOutcome, FinishConcertError>`. Cancel and Complete stay separate executors because each
  is one operation with its own validation, persistence, transaction, IO, and typed failure contract.
  There is **no** multi-operation `IConcertExecutor` combining them.

- **Steps** (`Application/Steps`) — the per-`DealType` unit of work an operation performs:
  `ICancelStep.ExecuteAsync(ConcertEntity)` and
  `ICompleteStep.ExecuteAsync(ConcertEntity) → UnitResult<FinishConcertError>`. The downstream Deal-dispatch
  plan classifies them: cancellation is a direct refund collaborator because every Deal case is identical;
  completion retains validated keyed release/payout implementations because it is one homogeneous operation
  with materially different effect graphs. Exact per-`DealType` coverage is registered at this module's own
  composition root.

The old `IConcertWorkflow`, `*Workflow` dependency-holders, `ConcertWorkflowBuilder`,
`ILifecycleTransitioner`, `IConcertStateMachineRegistry`, the reflection capability registry, and the
combined per-`DealType` `LifecycleStateMachine` no longer exist. Do not reintroduce them.

## The rule: when is it an Executor (and when is it just a service method)?

An Executor is warranted when the operation is a named Concert lifecycle transition and owns the command or
outcome that advances that lifecycle.

**Litmus test before you add one:** *"Is this a named operation in the lifecycle, or merely a guarded
mutation while the lifecycle remains unchanged?"* A non-lifecycle mutation belongs on the relevant
service (`ConcertService`), guarded and persisted directly, exactly like `ConcertService.PostAsync` /
`UpdateAsync`.

**Worked anti-example — declaring door revenue.** The venue declaring the night's door take:
- does **not** move the lifecycle machine (the gig stays `Posted`; settlement fires later off the sweep), and
- has **one** behaviour for every revenue-share type (load concert, guard, set a field, save).

So it is `ConcertService.DeclareDoorRevenueAsync` — a guarded mutation, not an executor. Likewise, "is this a
revenue-share settlement?" is already a real type (`Booking is DeferredBooking`), not a marker capability.
Don't invent a step, executor, or marker for a question the type system already answers.
