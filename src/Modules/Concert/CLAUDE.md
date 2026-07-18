# Concert module — the booking lifecycle machinery

The Concert module owns the application → booking → concert → settlement lifecycle. That lifecycle is a
**per-`DealType` state machine** driven by a small set of collaborating types. Before you add a type to
any of these families, understand what each one is *for* — they are not interchangeable, and the wrong
choice (an "executor" that isn't a transition, a "capability" that's a dead marker) is the exact
cargo-culting this doc exists to stop.

Read [`../../../docs/CODE_PATTERNS.md`](../../docs/CODE_PATTERNS.md) too — the keyed-strategy resolver
and the dependency-holder (`IConcertWorkflow` impls) patterns live there and are assumed here.

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
    `SettlementPayeeResolver` / `TicketPayeeResolver` (inverse maps).
  - **VAT invoice** → **`supplier`** (made the supply) / **`customer`** (billed). HMRC's legally-required
    words — you can't put "payee" on an invoice. Mapping: `supplier` = settlement payee, `customer` =
    ticket payee.

**`Party`** is the abstract "one side," and is **reserved for the invoice snapshot VO** (`InvoiceParty`:
a side's legal identity frozen at settlement). It is **not** a synonym for `tenant` — don't use the bare
word "party" as generic glue for "a venue/artist tenant" elsewhere.

The flip is the whole point: on `VenueHire` the venue is the supplier/settlement-payee; on every other
deal the artist is. That's why identity and role must stay separate words.

## The pieces

- **`LifecycleState` / `Trigger`** (`Domain/Lifecycle`) — the states an `ApplicationEntity` moves
  through and the events that move it. There is **one `LifecycleStateMachine` per `DealType`**, built by
  `ConcertWorkflowBuilder` and looked up via `IConcertStateMachineRegistry`. A `(state, trigger)` with no
  entry throws — an illegal transition is loud, never silent.

- **`ILifecycleTransitioner`** — the *only* thing that moves the machine: load the application, validate
  `(state, trigger)`, run an optional `effect`, mutate the state, persist. If the effect throws, no state
  change is saved.

- **Executor** (`Workflow/Executors`, one per `Trigger` — `Apply`, `Accept`, `Verify`, `Escrow`,
  `Settlement`, `Finish`, `Cancel`, …) — orchestrates **one lifecycle transition**. It calls
  `transitioner.TransitionAsync(appId, Trigger.X, effect)` where the `effect` resolves the per-`DealType`
  **workflow** (`IConcertWorkflowFactory.Create(dealType)`) and runs the matching **Step**. So an executor
  is the junction of two things: **a state transition** *and* **polymorphic dispatch over `DealType`**.
  `FinishExecutor` fires `Finish` and runs `workflow.Finish` — which is `PayoutFinishStep` for
  revenue-share, `ReleaseEscrowFinishStep` for fixed-fee. One executor, N step implementations.

- **Step** (`Workflow/Steps`, e.g. `IFinishStep`, `IAcceptStep`) — the per-`DealType` unit of work a
  transition performs. Implementations are registered and exposed as properties on each `IConcertWorkflow`
  (`FlatFeeWorkflow`, `DoorSplitWorkflow`, …). The workflow *is* the `DealType → steps` map.

- **Dispatcher** (`Workflow/Dispatchers`, interface in `Application/Interfaces`) — the Application-layer
  seam the module facade (`IConcertWorkflowModule`) and the payment event processors call; it forwards to
  the Infrastructure executor. It keeps callers depending on an Application interface, not a concrete
  Infrastructure executor. Usually thin — that's fine; it's a boundary, not logic.

- **Capability** (`Workflow/Capabilities`, e.g. `IAcceptsCheckout`, `IAppliesSimple`) — an interface a
  workflow *implements* to declare "this deal type supports X". Queried via
  `IConcertWorkflowCapabilityRegistry.Has<TCapability>(dealType)` — typically by API response mappers to
  gate HATEOAS links **without** instantiating the workflow. A capability must **expose a step or carry
  real behaviour**; a bare empty marker whose only job is to be reflected on is a smell — if the fact is
  already answerable from the type system, use that instead (see the door-revenue example below).

## The rule: when is it an Executor (and when is it just a service method)?

An Executor + Trigger + Dispatcher is warranted **only** when both are true:

1. the operation **fires a lifecycle transition** (moves the `LifecycleState` machine), **and**
2. its work **varies by `DealType`** (so it needs a per-type Step, resolved through the workflow).

**Litmus test before you add one:** *"Does this fire a `Trigger`, and does the work differ per
`DealType`?"* If the answer to either is no, it is **not** an executor — it's a plain method on the
relevant service (`ConcertService`, `BookingService`, …), guarded and persisted directly, exactly like
`ConcertService.PostAsync` / `UpdateAsync`.

**Worked anti-example — declaring door revenue.** The venue declaring the night's door take:
- does **not** move the lifecycle machine (the gig stays `Booked`; settlement fires later off the sweep), and
- has **one** behaviour for every revenue-share type (load concert, guard, set a field, save).

So it is `ConcertService.DeclareDoorRevenueAsync` — a guarded mutation. It was first (mis)built as an
`IDoorRevenueExecutor` + `DoorRevenueExecutor` + `IDoorRevenueDispatcher` + `DoorRevenueDispatcher`: four
types, one implementation, no transition, no per-type dispatch. That's throwaway ceremony — the executor
family is for transitions with polymorphic dispatch, and this was neither. Likewise, "is this a
revenue-share settlement?" is **not** a new `RequiresDoorRevenue` marker capability — it's already a real
type, `Booking is DeferredBooking` (DoorSplit/Versus use `DeferredBooking`, fixed-fee use
`StandardBooking`). Don't invent a marker for a question the type system already answers.
