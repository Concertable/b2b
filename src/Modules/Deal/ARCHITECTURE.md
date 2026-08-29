# Deal and lifecycle architecture

How the **deal** data and the **per-stage lifecycle** fit together. Read this before touching
`api/.../Modules/Deal/` or any of the `Application`, `Booking`, and `Concert` modules' `Domain/Lifecycle/`
and Deal-strategy code.

Two names that are easy to confuse, and that a past refactor deliberately separated:

- **Deal** — the *economic arrangement* (flat fee / door split / versus / venue hire), with its
  numbers (`Fee`, `HireFee`, `ArtistDoorPercent`, `Guarantee`) and its `PaymentMethod`. It is the
  editable current offer. Lives in the **Deal module** (`Modules/Deal/`), keyed by the `DealType` enum.
- **Contract** — the *signed binding artifact* (parties + both e-signatures + rendered legal terms +
  PDF), a frozen by-value snapshot formed at Accept. It is the `ContractEntity` in the **Concert
  module**, and is a different thing from the Deal it was rendered from.

---

## TL;DR

Two collaborating sub-systems, connected by a `DealType` enum value:

1. **The Deal module** owns the *data* — what kind of deal, with what numbers, on which
   `PaymentMethod`. Shape per deal type is fixed at compile time via a TPH (table-per-hierarchy)
   entity model in `Concertable.B2B.Deal.Domain`. It knows nothing about the lifecycle.
2. **The Concert workflow** owns the *behaviour* — how an application progresses from `Applied → … →
   Complete` for that deal type, who pays whom, when Stripe is called, and what each lifecycle step
   does. It lives entirely in the Concert module and reads deals through the `IDealModule` facade.

```
                Apply        Checkout       Accept (money leg)     Finish            Settle
  FlatFee       Simple       at Accept      capture → escrow       release escrow    —
  DoorSplit     Simple       at Accept      verify card (deferred) off-session payout  await settlement
  Versus        Simple       at Accept      verify card (deferred) off-session payout  await settlement
  VenueHire     Paid         at Apply       deposit → escrow       release escrow     —
```

---

## 1. The Deal module

```
api/.../Modules/Deal/
├─ Concertable.B2B.Deal.Domain/Entities/
│  ├─ DealEntity.cs                  (abstract TPH root: Id, PaymentMethod, abstract DealType)
│  ├─ FlatFeeDealEntity.cs           { Fee }
│  ├─ DoorSplitDealEntity.cs         { ArtistDoorPercent }
│  ├─ VenueHireDealEntity.cs         { HireFee }
│  └─ VersusDealEntity.cs            { Guarantee, ArtistDoorPercent }
├─ Concertable.B2B.Deal.Contracts/
│  ├─ DealType.cs                    enum { FlatFee, DoorSplit, Versus, VenueHire }
│  ├─ PaymentMethod.cs               enum { Cash, Transfer }
│  ├─ IDeal.cs                       interface (+ [JsonDerivedType] per subtype for the SPA wire)
│  ├─ FlatFeeDeal.cs / DoorSplitDeal.cs / …   records implementing IDeal
│  ├─ IDealModule.cs                 cross-module facade (Get / Create / Update / Delete)
│  └─ Strategies/                    IDealStrategy and shared factory contracts
├─ Concertable.B2B.Deal.Application/
│  ├─ Mappers/                       typed leaves + named DealMapper facade
└─ Concertable.B2B.Deal.Infrastructure/
   ├─ Services/Updaters/             typed leaves + named DealUpdater facade
   └─ EF configs, DbContext, repositories, and DI
```

Key invariants:

- **`DealEntity`** is a TPH base with `Id`, `PaymentMethod`, abstract `DealType`. Each subtype adds
  its own typed columns (`Fee`, `HireFee`, `ArtistDoorPercent`, `Guarantee`). Validation lives on the
  entity (`ValidateFee`, `ValidateArtistDoorPercent`).
- **`PaymentMethod`** (`Cash | Transfer`) is metadata for the off-platform settlement channel — it
  does **not** drive workflow timing. What decides "when money moves" is which lifecycle stage a step
  is wired to, not this field.
- **Deal strategy registration is vertical at each owning module's composition root.** `DealMapper` and
  `DealUpdater` delegate through the shared scoped `IDealStrategyFactory<T>` contract. The shared
  `DealStrategyBuilder` fixes the key to `DealType` and requires complete coverage for every registered
  family. Payment remains deal-type-agnostic.
- **`Concert.Opportunity.DealId`** is a satellite FK into the Deal module's DB (no nav back, no SQL FK
  across the context boundary). The Concert module reads deals through `IDealAccessor` /
  `IDealResolver` (§2.6), which delegate to `IDealModule`.

The `DealType` enum is load-bearing and assumed closed — every keyed-DI lookup, capability match, and
JSON polymorphic discriminator assumes a finite set known at compile time.

---

## 2. The lifecycle after the module carve

There is **no umbrella lifecycle, no combined `LifecycleState`, and no per-`DealType` state machine.** Each
persisted stage owns its own state in its own module, and authority only ever flows forward through
immutable Contracts handoffs (`AcceptedApplication` → Booking, `ConfirmedBooking` → Concert). A later stage
never reaches back to mutate an earlier aggregate, and no aggregate inspects another's state.

### 2.1 Per-module state, trigger, and machine

Application, Booking, and Concert each own `Domain/Lifecycle/{XState,XTrigger,XStateMachine}.cs`, named for
the module's own domain concept rather than the shared algorithm's generic vocabulary (the `state-machines`
skill's rule — contextual names, not `State`/`Trigger`):

| Module | State type | Trigger type |
|---|---|---|
| **Application** | `ApplicationState`: `Applied, Accepted, Rejected, Withdrawn, Cancelled` | `ApplicationTrigger`: `Accept, Reject, Withdraw, Cancel` |
| **Booking** | `BookingState`: `AwaitingConfirmation, ConfirmationFailed, Confirmed, CancellationPending, CancellationFailed, Cancelled` | `BookingTrigger`: `Confirm, RecordConfirmationFailure, BeginCancellation, RecordCancellationFailure, Cancel` |
| **Concert** | `ConcertState`: `Draft, Posted, CancellationPending, CancellationFailed, AwaitingSettlement, SettlementFailed, Complete, Cancelled` | `ConcertTrigger`: `Post, BeginCancellation, RecordCancellationFailure, Cancel, BeginSettlement, RecordSettlementFailure, CompleteSettlement` |

Each `XStateMachine` (e.g. `internal sealed class ApplicationStateMachine() : Concertable.Kernel.StateMachine<ApplicationState, ApplicationTrigger>(edges)`)
inherits the shared, stateless lookup algorithm directly (the `state-machines` skill) and configures its
legal edges through the base constructor — no wrapping field, no forwarded `Transition`. A defined edge
returns the next state; an undefined one returns `TransitionError<XState, XTrigger>`. The machines share
only that base algorithm — never a configured table, state, trigger, or error. `DealType` changes the
*behaviour inside* a stage, never the legal edges, so there is exactly one configured machine per owning
module.

### 2.2 The aggregate owns its transition

There is no `ConcertWorkflowBuilder`, `IConcertStateMachineRegistry`, or `ILifecycleTransitioner`. Each
aggregate (`ApplicationEntity`, `BookingEntity`, `ConcertEntity`) holds one `private static readonly
XStateMachine` and mutates only through a private helper — Concert's, for example:

```csharp
private Result<ConcertState, TransitionError<ConcertState, ConcertTrigger>> Transition(ConcertTrigger trigger)
{
    var transition = stateMachine.Transition(State, trigger);
    if (transition.TryGetValue(out var next))
        State = next;
    return transition;
}
```

`State` has a private setter. Callers invoke the semantic operations — Application's `Accept`/`Reject`/
`Withdraw`, Booking's confirmation/cancellation operations, Concert's `Post`/`BeginCancellation`/`Cancel`/
`BeginSettlement`/`CompleteSettlement` — never a public generic `Transition`. Each operation runs its
prerequisite validation, calls the private helper, and on the success value mutates auxiliary data and
raises domain events; a rejected edge returns early, leaving state, auxiliary facts, and events unchanged.
`LifecycleStateOwnershipTests` (architecture suite) mechanically fails any `State` assignment outside that
helper, and any EF bulk-update that would bypass it.

### 2.3 Transition failures are operation-owned errors

An operation composes the rejected `TransitionError<XState, XTrigger>` into its own closed error union
rather than throwing: each error type (e.g. `AcceptApplicationError`, `CancelBookingError`,
`CancelConcertError`, `PostConcertError`, `FinishConcertError`) declares an
`InvalidTransition(TransitionError<XState, XTrigger>)` case alongside its other expected failures. There is
no shared error base, `NotFound` base, or `IError` widening. An operation with no failure beyond the
rejected edge returns the transition error directly.

### 2.4 Each module owns its own operations — there is no cross-module workflow

The old cross-module `IConcertWorkflow`, its concrete `*Workflow` dependency-holders, the workflow
factory, the checkout dispatcher, and the capability registry are gone. No type spans the stages.
Instead each module owns the operations that act on its own aggregate, each through its own module-local
executable workflow:

- **Application** owns apply, checkout, accept, reject, and withdraw, through `IApplicationWorkflow`
  (Apply and Accept). Accept produces the immutable `AcceptedApplication` handoff
  (`Application.Contracts`) that Booking consumes.
- **Booking** owns confirmation, payment failure/retry, and pre-Concert cancellation, plus Booking and
  Contract formation from the accepted handoff, through `IBookingWorkflow`.
- **Concert** owns creation from the `ConfirmedBooking` handoff, cancellation, and completion/settlement —
  its own module-local `IConcertWorkflow` (unrelated to the old cross-module type of the same name;
  Cancel and Complete, §2.5) plus the uniform `IConcertService.CreateAsync(ConfirmedBooking)`.

### 2.5 Deal-varying methods use honest method-header unions, not a keyed workflow

Deal-varying methods are classified by invocation shape, per module, with no shared registry:

- A **same-interface family** such as Application's deal-terms rendering is selected through the shared
  `IDealStrategyFactory<TStrategy>`. `DealStrategyBuilder` proves complete `DealType` coverage at the owning
  module's composition root.
- A **heterogeneous method** gets one interface per honest method header and a Dunet union over those
  interfaces. `DealUnionBuilder<TUnion>` composes the generic keyed-union builder with `DealType` and
  `IDealStrategy`, requiring exactly one case for every deal type. Application accept is the worked example:
  `IAccept` covers the header without a payment-method parameter, while `IAcceptPaid` covers the header that
  requires one. FlatFee and VenueHire map to `IAccept`; DoorSplit and Versus map to `IAcceptPaid`. The caller
  matches the interface-bearing union case, never the four Deal cases:

```csharp
return acceptFactory.Create(deal.DealType) switch
{
    Accept.Standard(var accept) => accept.Accept(facts, application, deal),
    Accept.Paid when string.IsNullOrWhiteSpace(paymentMethodId) =>
        new AcceptApplicationError.PaymentMethodRequired(),
    Accept.Paid(var accept) => accept.Accept(facts, application, deal, paymentMethodId),
    _ => throw new UnreachableException()
};
```

Concert's homogeneous Cancel/Complete strategies (`ICancel`, `IComplete`, under
`Strategies/<Operation>`) use the shared Deal strategy factory. `IKeyedServiceProvider` never escapes
into a consumer. Concert creation is uniform across `DealType` and selects no keyed strategy.

### 2.6 How the Concert module reads deal terms

A single `internal sealed class DealAccessor : IDealAccessor, IDealResolver`
(`Infrastructure/Services/DealAccessor.cs`), registered request-scoped and aliased so both interfaces
resolve to the *same* instance:

- **`IDealResolver`** (write side, used by executors): `ResolveByOpportunityIdAsync` /
  `…ApplicationIdAsync` / `…ConcertIdAsync`. Each maps entity id → `DealId` (via a repository's
  `GetDealIdByIdAsync`) → `IDealModule.GetByIdAsync(dealId)`, **memoizing** the result — first resolve
  wins.
- **`IDealAccessor`** (read side, used by steps): a single `IDeal Deal` property that returns the
  memoized deal, or throws `InvalidOperationException` ("No deal resolved this scope …") if the
  orchestrator hasn't resolved one yet. Steps cast to the concrete type (e.g.
  `(FlatFeeDeal)dealAccessor.Deal`).

So the contract is: the executor resolves the deal, then the step reads it. (This request-scoped
memoizer replaced an earlier `IContractLoader` design — that type no longer exists.)

### 2.7 Money movement

Steps call two Payment facades from `Concertable.Payment.Client`: **`IEscrowClient`**
(`DepositAsync`, `CaptureAsync`, `ReleaseByBookingIdAsync`, `RefundByBookingIdAsync`) and
**`IManagerPaymentClient`** (`PayAsync` off-session, `CreateSetupSessionAsync`,
`CreateVerifySessionAsync`, `CreateHoldSessionAsync`, `FindHeldIntentAsync`). Amounts flow tenant →
tenant, sourced from the frozen tenant snapshot on the application/booking.

| Deal | Checkout session | Accept-step money | Finish-step money |
|---|---|---|---|
| **FlatFee**   | `HoldCheckoutStep` → hold `deal.Fee` (venue pre-auth) | `CaptureEscrowAcceptStep`: `FindHeldIntentAsync` → `CaptureAsync` (venue→artist) into escrow | `ReleaseEscrowFinishStep`: `ReleaseByBookingIdAsync` → artist |
| **VenueHire** | `SetupCheckoutStep` (apply-time) → setup `deal.HireFee` (artist off-session) | `DepositEscrowAcceptStep`: `DepositAsync` (artist→venue) into escrow off-session | `ReleaseEscrowFinishStep`: `ReleaseByBookingIdAsync` → venue |
| **DoorSplit** | `VerifyCheckoutStep` → `CreateVerifySessionAsync` (venue card verify) | `PaidAcceptStep`: no charge — `CreateDeferredAsync` stores the card | `PayoutFinishStep`: off-session `PayAsync` (venue→artist), `artistShare = rev × ArtistDoorPercent` |
| **Versus**    | `VerifyCheckoutStep` → `CreateVerifySessionAsync` | `PaidAcceptStep`: no charge (deferred) | `PayoutFinishStep`: off-session `PayAsync`, `artistShare = Guarantee + rev × ArtistDoorPercent` |

Escrow deals (FlatFee, VenueHire) confirm money **at Accept** and release **at Finish**
(`Booked +Finish→Complete`). Payout deals (DoorSplit, Versus) ring-fence nothing at Accept (verify +
store card) and pay off-session **at Finish** (`Booked +Finish→AwaitingSettlement`, then
`SettlementPaymentSucceeded→Complete`). Cancellation always refunds escrow. Settlement gross comes
from `ISettlementAmountResolver`, whose named facade selects one keyed strategy through
`IConcertDealStrategyFactory`. The DoorSplit and Versus leaves share the revenue-loading base that
reads ticket revenue plus declared door revenue, then apply their own percentage-only or
guarantee-plus-percentage formula. `PayoutFinishStep` and `InvoiceIssuer` consume that same facade so
charged and invoiced amounts cannot diverge. Payment webhooks return as integration events
(`PaymentSucceeded/FailedEvent`) handled by the `*Processor` classes, which route by
`Metadata["type"]` and drive the matching executor or `VerifyCoordinator`;
idempotency is provided by the inbox.

### 2.8 Ticket payee vs settlement payee

`DealPayeeResolver` implements the cohesive `IDealPayeeResolver` facade. Its generic strategy factory
selects one directional leaf per `DealType`: FlatFee/DoorSplit/Versus use
`VenuePaysArtistDealPayeeResolver` (the venue keeps ticket revenue and the artist receives settlement);
VenueHire uses `ArtistPaysVenueDealPayeeResolver` (the artist keeps ticket revenue and the venue
receives settlement). The facade returns the ticket user, ticket tenant, or settlement tenant directly,
so consumers never branch on deal type or invert one role to infer another.

---

## 3. The lifecycle entities

| Entity | Owns lifecycle `State`? | Role | TPH subtypes |
|---|---|---|---|
| `ApplicationEntity` | **Yes** — `Application.Lifecycle.ApplicationState` | Terminal after its own decision (accept/reject/withdraw) | `StandardApplication`, `PrepaidApplication { PaymentMethodId }` (VenueHire) |
| `BookingEntity` | **Yes** — `Booking.Lifecycle.BookingState` | Owns confirmation, failure/retry, and pre-Concert cancellation | `StandardBooking`, `DeferredBooking { PaymentMethodId }` (DoorSplit/Versus) |
| `ConcertEntity` | **Yes** — `Concert.Lifecycle.ConcertState` | The live concert: draft/post, cancellation, settlement, completion | (single type) |
| `ContractEntity` | No | The signed binding artifact (see below) | (single type) |

FK chain: `OpportunityEntity (1)→(N) ApplicationEntity (1)→(0..1) BookingEntity (1)→(0..1)
ConcertEntity`, and `BookingEntity (1)→(0..1) ContractEntity`. `OpportunityEntity` is `ITenantScoped`
(the venue) and holds the satellite `DealId` FK into the Deal module.

The TPH split on Application/Booking exists so prepaid-at-apply (VenueHire) and deferred-pay-at-finish
(DoorSplit/Versus) can carry a `PaymentMethodId` without nullable columns on the standard variants.

**`ContractEntity`** (`Concert.Domain/Entities/ContractEntity.cs`) is a by-value immutable snapshot
(all private setters): `BookingId`, `VenueId`/`VenueName`, `ArtistId`/`ArtistName`, `Period`,
`DealType`, `PaymentMethod`, `TermsText` (rendered legal prose), `PlatformTermsVersion`,
`ArtistESignature` + `VenueESignature`, `PdfBlobName` (assigned in `Create`),
`CreatedAtUtc`. It is created by **`ContractIssuer.IssueAsync`** (`Infrastructure/Services/`), invoked
from `AcceptExecutor` during the Accept transition: it renders terms via `IDealTermsRenderer`, copies
the artist's e-signature (captured at apply) and the venue's (from the accept request), and persists
via `IContractRepository`. The Deal is the *editable* current offer; the
Contract is the *frozen, signed copy* — "formed at Accept" is a convention of the workflow, not a
model-enforced invariant. `ESignature` is a `sealed record` (`UserId, AtUtc, Ip, UserAgent?,
SignatoryName, DrawnSignatureImage?`), attributed server-side — the `Ip` is required (fail-closed at
capture), the client-supplied `UserAgent` stays optional.

---

## 4. Adding a new deal type

A new deal type touches each module's Deal-varying leaves, not a shared workflow. The lifecycle machines are
`DealType`-agnostic (the legal edges never vary by deal) and need no changes; only the per-module method
implementations and strategy leaves do. The Deal-specific builders enforce complete coverage, so an
unhandled new type fails composition/tests in each owning module.

1. **`Deal.Contracts`** — add the case to `DealType.cs`; add an `XDeal : IDeal` record + a
   `[JsonDerivedType]` line on `IDeal.cs`.
2. **`Deal.Domain` / `.Application` / `.Infrastructure`** — add `XDealEntity : DealEntity` (typed
   columns + `Create`/`Update`/validator), an `XDealMapper`, an `XDealUpdater`, and an EF config. Add
   both strategy leaves to the new deal's vertical `strategies.For(DealType.X)` block; the builder's
   coverage gate fails until both families are present.
3. **Migrations** — re-scaffold: run `./initial-migrations.ps1` from `api/` (per the `migrations` skill; never
   an additive migration).
4. **Per-module steps** — in each stage that behaves differently for the new deal, reuse an existing step
   where the money shape fits or add a concrete implementation of the module's step/method-header interface
   (Application accept/checkout, Booking confirm/cancel, Concert cancel/complete) only where the movement is
   genuinely new.
5. **Per-module registration** — register the new implementation against an honest method-header union case
   or a same-interface strategy family in the owning module's composition root. The builder discovers the new
   enum member and fails until the mapping exists. No workflow type exists to add.
6. **Deal strategy leaves** — add the deal's mapper/updater/terms/settlement leaves to the vertical
   `strategies.For(DealType.X)` block in the module that owns each family; the builder's exact-coverage gate
   fails until every family is present. A revenue-share settlement leaf reuses the shared revenue-loading
   base and owns its complete formula.
7. **Payment** — keep it deal-type-agnostic. Compose its existing escrow/session/payment client
   operations from the owning module's steps; do not register a Payment strategy keyed by B2B's `DealType`.
8. **Frontend** — add the deal form + accept/apply checkout UI variant.

---

## 5. Could this support custom / drag-and-drop deals?

**Short answer:** not in its current shape — but the workflow scaffold is closer than it looks. The
blocker is the *data* side (a closed `DealType`, typed TPH columns, typed step reads), not the
*behaviour* side (the capability + workflow-builder pattern already composes cleanly).

### 5.1 What stands in the way

| Concern | Where | Why it blocks dynamic deals |
|---|---|---|
| `DealType` is a closed enum | `Deal.Contracts/DealType.cs` | Every keyed-DI lookup, capability match, and JSON discriminator assumes a finite compile-time set. User-defined deals need an open identifier + runtime registration. |
| TPH schema per subtype | `Deal.Domain/Entities/*DealEntity.cs` + EF configs | Each deal type gets its own columns; a user-defined deal has unknown shape at migration time (needs a JSON blob or rule list). |
| Strategy leaves read typed properties | `Concert.Infrastructure/Services/Settlement/*SettlementAmount.cs` | Revenue-share leaves read `ArtistDoorPercent` and optional `Guarantee`; a custom deal has no typed property — you'd need a rule interpreter or a finite set of rule kinds. |
| Stripe primitives are rigid | Payment | Connect exposes a small finite set of operations; custom deals still map onto that set. |
| `DealPayeeResolver` selects a closed directional strategy | `Concert.Application/Resolvers/` | Who keeps ticket revenue and who receives settlement are cohesive values keyed by `DealType`; a custom deal must declare both. |

### 5.2 Realistic options

- **Option A — keep the closed shape, make adding types cheaper.** Adding a developer-defined type is
  already largely mechanical (§4). QoL wins: move the share formula to a single home (§6.1) and keep
  every per-type strategy family in the vertical registration block.
- **Option B (recommended if drag-and-drop is the goal) — one `Composite` deal type.** Add a single
  `DealType.Composite` whose `CompositeDealEntity` stores a JSON *template* (a list of `Rule`s: kind,
  amount expression, payer/payee, trigger state); a `CompositeWorkflow` whose steps **interpret** the
  template against a finite rule vocabulary (`FlatCharge`, `PercentSplit`, `Guarantee`, `Hold`,
  `Release`, `Refund`) — which is exactly the SPA's drag-and-drop palette. Keeps the four built-ins
  unchanged, needs no per-deal migration (one JSON column), maps cleanly to Stripe primitives, and can
  be built incrementally.
- **Option C — open the `DealType` identifier entirely** (string/Guid + template table + runtime DI +
  generic factory). Workable but invasive (breaks the JSON discriminator, touches many files); only
  worth it if Option B proves too restrictive.

---

## 6. Frequently confused things & open issues

- **`Deal` ≠ `Contract`.** The Deal is the editable economic offer (Deal module); the `ContractEntity`
  is the frozen signed artifact formed at Accept (Concert module). Different lifetimes, different
  models.
- **`PaymentMethod` ≠ `paymentMethodId`.** `PaymentMethod` is the Deal-domain enum (`Cash | Transfer`)
  used for accounting; `paymentMethodId` is a Stripe PM id (`pm_…`) flowed through the paid steps and
  the `Prepaid`/`Deferred` TPH variants. Different things.
- **Operation/executor interfaces are each module's own Application contracts** — no longer all
  Concert-internal. HTTP services, controllers, workers, and payment processors bind directly to the owning
  module's interface. Payment verification outcomes persist before the accept/payment join advances the
  Booking; the join is Booking-owned and correlated to the accepted Application and payment operation.
- **Strategy builders run at each module's composition root**, not per request; there is no
  `ConcertWorkflowBuilder` and no shared workflow/state-machine registry — each module wires its own
  strategies and its one lifecycle machine.

### 6.1 Settlement amount has one runtime home

`ISettlementAmountResolver` is the single settlement-gross contract used by payout and invoicing.
Its generic strategy factory selects FlatFee, DoorSplit, Versus, or VenueHire leaves from the vertical
registration. DoorSplit and Versus share only revenue loading; each leaf owns its complete formula.
Deal entities carry economic inputs and validation but do not duplicate the runtime calculation.

### 6.2 Deal strategy mappings are module-local

Shared B2B Infrastructure owns the scoped Deal strategy and union factories plus the Deal-specific builders
that compose the business-agnostic keyed builders. Each module owns only its strategy implementations and
the mappings at its composition root. Named facades remain the method-specific API, and keyed lookup stays
inside the shared factories. Payment remains unaware of `DealType`.
