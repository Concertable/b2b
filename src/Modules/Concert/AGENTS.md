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

There is no per-`DealType` state machine and no cross-module workflow object. Concert owns one configured
machine for its own stage, in `Domain/Lifecycle/`:

- **`ConcertState`** — `Draft, Posted, CancellationPending, CancellationFailed, AwaitingSettlement,
  SettlementFailed, Complete, Cancelled`.
- **`ConcertTrigger`** — `Post, BeginCancellation, RecordCancellationFailure, Cancel, BeginSettlement,
  RecordSettlementFailure, CompleteSettlement`.
- **`ConcertStateMachine`** — `internal sealed class ConcertStateMachine : Concertable.Kernel.StateMachine<ConcertState, ConcertTrigger>`,
  inheriting the shared algorithm directly (the `state-machines` skill owns it) and configuring its sixteen
  legal edges through the base constructor. It stores no entity state; a rejected edge returns
  `TransitionError<ConcertState, ConcertTrigger>`.

`ConcertEntity` holds `State` with a private setter and one `private static readonly ConcertStateMachine`.
Every lifecycle mutation funnels through the aggregate's private `Transition(ConcertTrigger)` helper, which
assigns `State = next` **only** from the success value, then mutates operation-specific data and raises
domain events; a rejected transition leaves state, auxiliary facts, and events untouched. Callers invoke the
semantic operations (`Post`, `BeginCancellation`, `Cancel`, `BeginSettlement`, `RecordSettlementFailure`,
`CompleteSettlement`) — never a public generic `Transition`. `LifecycleStateOwnershipTests` in the
architecture suite mechanically fails any `State` assignment outside that private path.

Operation errors are operation-owned closed unions (`PostConcertError`, `CancelConcertError`,
`FinishConcertError`), each carrying `InvalidTransition(TransitionError<ConcertState, ConcertTrigger>)` for a
rejected edge and its own additional expected cases. There is no shared error base or `IError` widening.

## The pieces

- **Creation** — `IConcertService.CreateAsync(ConfirmedBooking)` owns uniform draft creation from the
  immutable Booking handoff. It is the same projection-lookup/genre-intersection/persist/notify/email path
  for every `DealType`; the immutable terms cases supply different data to `ConcertEntity.CreateDraft`, they
  do not select different creation behaviour. Creation stays on `IConcertService` and performs no Deal selection.
  Post-commit notification is staged through the outbox and delivered only after the shared confirmation
  transaction commits.

- **Workflow** (`Application/Interfaces/IConcertWorkflow`, `Infrastructure/Services/ConcertWorkflow`) — the
  module-local executable coordinator for Cancel and Complete. It owns each operation's loading, validation,
  transaction boundary, Deal selection, persistence, IO, and typed failure contract. HTTP entry points begin
  at `IConcertService`; background completion invokes the workflow directly through a fresh scope.

- **Deal-selected implementations** (`Application/Strategies/<Operation>` contracts,
  `Infrastructure/Strategies/<Operation>` implementations) — `ICancel.CancelAsync(ConcertEntity)` and
  `IComplete.CompleteAsync(SettlementPreparation.Ready)`. Each is a homogeneous operation selected through
  `IDealStrategyFactory<T>`, with exact per-`DealType` coverage registered at this module's composition root.

The old cross-stage `IConcertWorkflow` dependency-holder, `ConcertWorkflowBuilder`,
`ILifecycleTransitioner`, `IConcertStateMachineRegistry`, reflection capability registry, and combined
per-`DealType` `LifecycleStateMachine` no longer exist. Do not reintroduce those shapes.

## The rule: when does work belong in the workflow?

A method belongs in `IConcertWorkflow` when it is a named Concert lifecycle operation and owns the command or
outcome that advances that lifecycle.

**Litmus test before you add one:** *"Is this a named operation in the lifecycle, or merely a guarded
mutation while the lifecycle remains unchanged?"* A non-lifecycle mutation belongs on the relevant
service (`ConcertService`), guarded and persisted directly, exactly like `ConcertService.PostAsync` /
`UpdateAsync`.

**Worked anti-example — declaring door revenue.** The venue declaring the night's door take:
- does **not** move the lifecycle machine (the gig stays `Posted`; settlement fires later off the sweep), and
- has **one** behaviour for every revenue-share type (load concert, guard, set a field, save).

So it is `ConcertService.DeclareDoorRevenueAsync` — a guarded mutation, not a workflow operation. Likewise,
"is this a revenue-share settlement?" is already a real type (`Concert is DoorRevenueConcert`), not a marker capability.
Don't invent a strategy contract or marker for a question the type system already answers.
