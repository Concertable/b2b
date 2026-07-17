# Deal & Concert-Workflow Architecture

How the **deal** data and the **concert lifecycle workflow** fit together. Read this before touching
`api/.../Modules/Deal/`, `api/.../Modules/Concert/Concertable.B2B.Concert.Application/Workflow/`, or
`api/.../Modules/Concert/Concertable.B2B.Concert.Infrastructure/Services/Workflow/`.

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
│  ├─ DoorSplitDealEntity.cs         { ArtistDoorPercent, CalculateArtistShare(rev) }
│  ├─ VenueHireDealEntity.cs         { HireFee }
│  └─ VersusDealEntity.cs            { Guarantee, ArtistDoorPercent, CalculateArtistShare(rev) }
├─ Concertable.B2B.Deal.Contracts/
│  ├─ DealType.cs                    enum { FlatFee, DoorSplit, Versus, VenueHire }
│  ├─ PaymentMethod.cs               enum { Cash, Transfer }
│  ├─ IDeal.cs                       interface (+ [JsonDerivedType] per subtype for the SPA wire)
│  ├─ FlatFeeDeal.cs / DoorSplitDeal.cs / …   records implementing IDeal
│  ├─ IDealModule.cs                 cross-module facade (Get / Create / Update / Delete)
│  └─ IDealStrategy.cs               empty marker for keyed strategies
└─ Concertable.B2B.Deal.Application/ (services, mappers, updaters)
   Concertable.B2B.Deal.Infrastructure/ (EF configs, DbContext, updaters, DI)
```

Key invariants:

- **`DealEntity`** is a TPH base with `Id`, `PaymentMethod`, abstract `DealType`. Each subtype adds
  its own typed columns (`Fee`, `HireFee`, `ArtistDoorPercent`, `Guarantee`). Validation lives on the
  entity (`ValidateFee`, `ValidateArtistDoorPercent`).
- **`PaymentMethod`** (`Cash | Transfer`) is metadata for the off-platform settlement channel — it
  does **not** drive workflow timing. What decides "when money moves" is which lifecycle stage a step
  is wired to, not this field.
- **`IDealStrategy`** is currently only a marker: the sole extender is Payment's
  `IStripeValidationStrategy` (keyed-DI by `DealType` — Account vs Customer onboarding rules per deal).
- **`Concert.Opportunity.DealId`** is a satellite FK into the Deal module's DB (no nav back, no SQL FK
  across the context boundary). The Concert module reads deals through `IDealAccessor` /
  `IDealResolver` (§2.6), which delegate to `IDealModule`.

The `DealType` enum is load-bearing and assumed closed — every keyed-DI lookup, capability match, and
JSON polymorphic discriminator assumes a finite set known at compile time.

---

## 2. The Concert workflow

The lifecycle lives on **`ApplicationEntity.State`** — one state machine per deal type. A request
enters via `controller → IApplicationService`/`IConcertWorkflowModule → *Dispatcher → *Executor →
ILifecycleTransitioner → the deal-type's IConcertWorkflow step`. Deal *terms* are read through a
request-scoped `IDealAccessor`; money movement is delegated to Payment via `IEscrowClient` /
`IManagerPaymentClient`.

### 2.1 Lifecycle state + trigger

`Concert.Domain/Lifecycle/LifecycleState.cs` — 10 states:

```csharp
enum LifecycleState {
    Applied, Rejected, Withdrawn,
    Accepted,            // accept landed; payment leg pending (which leg = deal type)
    PaymentFailed,       // accept-leg payment failed (verify hold / escrow capture) — retryable
    Booked,              // payment confirmed, draft created — CanPost gate
    AwaitingSettlement,  // deferred payout leg (DoorSplit / Versus)
    SettlementFailed,    // post-Finish payout failed — recovery lands Complete, not Booked
    Complete,
    Cancelled,           // booking killed while escrow Held — escrow refunded, concert dead
}
```

`Concert.Domain/Lifecycle/Trigger.cs` — 11 triggers: `Accept, Reject, Withdraw,
VerifyPaymentSucceeded/Failed, EscrowPaymentSucceeded/Failed, SettlementPaymentSucceeded/Failed,
Finish, Cancel`.

`LifecycleStateMachine` (`Domain/Lifecycle/`) wraps a
`FrozenDictionary<(LifecycleState, Trigger), LifecycleState>`; `Next(current, trigger)` returns the
target or throws `ConflictException("Cannot {trigger} from {current}")`. The state machine carries no
identity of its own — the *table content* is what makes it deal-type-specific.

### 2.2 The transition table is per-deal-type, assembled by a fluent builder

`ConcertWorkflowBuilder` (`Infrastructure/Services/Workflow/`) accumulates edges into a dictionary via
`With*` methods, each adding a fixed slice; `.Build()` wraps the dictionary in a
`LifecycleStateMachine` and registers `(DealType → stateMachine, workflowType)`. `Add` throws on a
duplicate `(from, trigger)`, so overlapping slices are a startup error.

Edge slices: `WithApply` (`Applied +Accept→Accepted`, `+Reject→Rejected`, `+Withdraw→Withdrawn`);
`WithEscrowPayment` (`Accepted/PaymentFailed +EscrowSucceeded→Booked`, `+EscrowFailed→PaymentFailed`,
plus idempotent `Cancelled +Escrow*→Cancelled` late-webhook self-loops); `WithVerifiedPayment`
(the equivalent using the Verify triggers); `WithFinish(to)` (`Booked +Finish→to`); `WithSettlement`
(`AwaitingSettlement/SettlementFailed +Settlement*→Complete/SettlementFailed`); `WithCancel`
(`Booked +Cancel→Cancelled`); `WithApplicationCancel` (`Accepted/PaymentFailed +Withdraw/Cancel→
Cancelled`).

The graphs therefore **differ by deal type** (composed in `AddConcertWorkflows()`,
`Infrastructure/Extensions/ServiceCollectionExtensions.cs`):

| Deal type | Accept-leg triggers | `WithFinish(to)` | Finish leg | Settlement states |
|---|---|---|---|---|
| **FlatFee**   | `WithEscrowPayment`   | `Complete`           | `ReleaseEscrowFinishStep` | none |
| **VenueHire** | `WithEscrowPayment`   | `Complete`           | `ReleaseEscrowFinishStep` | none |
| **DoorSplit** | `WithVerifiedPayment` | `AwaitingSettlement` | `PayoutFinishStep`        | `WithSettlement` |
| **Versus**    | `WithVerifiedPayment` | `AwaitingSettlement` | `PayoutFinishStep`        | `WithSettlement` |

`AddConcertWorkflows` registers two singletons off the accumulated maps:
`IConcertStateMachineRegistry → ConcertStateMachineRegistry` (`FrozenDictionary<DealType,
LifecycleStateMachine>`, `Get(type)`) and `IConcertWorkflowCapabilityRegistry →
ConcertWorkflowCapabilityRegistry` (`DealType → workflow CLR type`; `Has<TCapability>(dealType)` tests
`IsAssignableTo`). Workflow instances themselves are keyed-scoped:
`AddKeyedScoped<IConcertWorkflow, TWorkflow>(dealType)`.

### 2.3 `ILifecycleTransitioner` — the atomic transition

Interface `Application/Workflow/ILifecycleTransitioner.cs`; impl
`Infrastructure/Services/Workflow/LifecycleTransitioner.cs`. It also declares
`internal delegate Task TransitionEffect(ApplicationEntity application)`.

`TransitionAsync(int applicationId, Trigger trigger, TransitionEffect? effect = null)` advances one
application's state for a trigger, running an optional side-effect **inside** the same transition:

1. Load the application; `.OrNotFound()`.
2. `machines.Get(application.DealType)` → the deal-type state machine.
3. `machine.Next(application.State, trigger)` — **validates** the transition is legal (throws if not),
   as a guard *before* any effect runs.
4. If `effect` is non-null, `await effect(application)` — the real work (payment capture, booking
   creation, contract issue, …).
5. `application.Transition(trigger, machine)` — flips `State`.
6. `SaveChangesAsync()`.

Ordering matters: the guard runs first, the money effect second, the state flip + save last — so a
throwing effect leaves the DB state unmoved.

### 2.4 Executors and dispatchers (the two-layer split)

An **executor** holds the real orchestration (calls `ILifecycleTransitioner`,
`IConcertWorkflowFactory`, repositories, `IDealResolver`). A **dispatcher** is a razor-thin
`internal sealed` adapter implementing an `Application/Interfaces/I*Dispatcher` contract and forwarding
1:1 to its executor — so the Application layer / `IConcertWorkflowModule` depends on an interface while
the orchestration stays in Infrastructure. **Dispatcher calls executor.** The one exception is
`CheckoutDispatcher`, which contains real logic (§2.5).

| Executor | Responsibility | Entry point |
|---|---|---|
| `ApplyExecutor` | Create the `ApplicationEntity` (simple or paid), snapshot both tenant ids + terms fingerprint + artist e-signature. Does **not** go through the transitioner (initial state `Applied`). | `ApplicationController.Apply` |
| `AcceptExecutor` | The `Accept` transition: resolve deal, verify terms unchanged, run the deal's Accept step, link booking, **issue the `ContractEntity`**, background-reject other applications + render the PDF. | `ApplicationController.Accept` |
| `RejectExecutor` / `WithdrawExecutor` / `CancelApplicationExecutor` | `Reject` / `Withdraw` / `Cancel` on an application (pre-concert). Withdraw/Cancel from `Accepted`/`PaymentFailed` run `IApplicationCancelStep` (escrow refund). | `ApplicationController.*` |
| `VerifyExecutor` / `EscrowExecutor` | Payment-outcome callbacks (`VerifyPayment*` / `EscrowPayment*`). On success run the deal's `Book` step; late events on a `Cancelled` app are no-ops (or compensating refund). | `IConcertWorkflowModule.{Verify,Escrow}SucceededAsync` ← Stripe `*Processor`s |
| `SettlementExecutor` | `SettlementPayment*`, state-only. | `IConcertWorkflowModule.SettlementSucceededAsync` ← `SettlementPaymentProcessor` |
| `FinishExecutor` | `Finish`: guard concert has ended, resolve deal, run the deal's `Finish` step. | `IConcertWorkflowModule.FinishAsync` ← `ConcertCompletionRunner` (batch) |
| `CancelExecutor` | `Cancel` a booked concert: run the deal's `Cancel` step + `concert.Cancel()`. | `IConcertWorkflowModule.CancelAsync` ← `ConcertController` |

Dispatch never uses `switch(dealType)`: deal-type routing is always either keyed-DI resolution
(`IConcertWorkflowFactory.Create(type)`) or capability-interface pattern-matching.

### 2.5 Steps and capabilities

A **step** is the unit of deal-type-specific behaviour at one point in the lifecycle. Interfaces in
`Application/Workflow/Steps/`, all marker-derived from `IConcertStep`:

| Interface | Method |
|---|---|
| `ISimpleApplyStep` / `IPaidApplyStep` | `ApplyAsync(…) → ApplicationEntity` (paid variant also takes `paymentMethodId`) |
| `IApplyCheckoutStep` / `IAcceptCheckoutStep` | `ExecuteAsync(…) → Checkout` (pre-apply / pre-accept Stripe session) |
| `ISimpleAcceptStep` / `IPaidAcceptStep` | `ExecuteAsync(applicationId[, paymentMethodId])` |
| `IBookStep` / `IFinishStep` / `ICancelStep` | `ExecuteAsync(bookingId | concertId)` |
| `IApplicationCancelStep` | `ExecuteAsync(applicationId)` — global, not `IConcertStep`-derived |

`IConcertWorkflow` (`Application/Workflow/IConcertWorkflow.cs`) exposes only the three *universal*
steps every deal has — `DealType Type`, `IBookStep Book`, `IFinishStep Finish`, `ICancelStep Cancel`.
Apply / Accept / Checkout are **not** on the base interface; they attach via **capability interfaces**
(`Application/Workflow/Capabilities/`) that each concrete workflow additionally implements:
`IAppliesSimple`/`IAppliesPaid`/`IAppliesCheckout` and `IAcceptsSimple`/`IAcceptsPaid`/
`IAcceptsCheckout`. This is why executors pattern-match — e.g. `AcceptExecutor` does
`workflow switch { IAcceptsPaid w when pm != null => …, IAcceptsSimple w => …, _ => throw }`, and
`CheckoutDispatcher` matches `IAppliesCheckout` (apply-time) / `IAcceptsCheckout` (accept-time),
throwing `BadRequestException` if the deal lacks the capability.

Concrete workflows (`Infrastructure/Services/Workflow/Workflows/`):

| Workflow | Capabilities | Apply / Checkout / Accept steps |
|---|---|---|
| `FlatFeeWorkflow`   | `IAppliesSimple, IAcceptsCheckout, IAcceptsSimple` | `SimpleApplyStep`, `HoldCheckoutStep` (accept), `CaptureEscrowAcceptStep` |
| `DoorSplitWorkflow` | `IAppliesSimple, IAcceptsCheckout, IAcceptsPaid`   | `SimpleApplyStep`, `VerifyCheckoutStep`, `PaidAcceptStep` |
| `VersusWorkflow`    | `IAppliesSimple, IAcceptsCheckout, IAcceptsPaid`   | `SimpleApplyStep`, `VerifyCheckoutStep`, `PaidAcceptStep` |
| `VenueHireWorkflow` | `IAppliesPaid, IAppliesCheckout, IAcceptsSimple`   | `PaidApplyStep`, `SetupCheckoutStep` (**apply**), `DepositEscrowAcceptStep` |

Note the asymmetry: FlatFee/DoorSplit/Versus check out at **accept** time; VenueHire checks out at
**apply** time (the artist is the payer and is present at apply). All four inject the shared
`CreateConcertDraftStep` (Book) and `RefundEscrowStep` (Cancel); `SimpleApplyStep` is reused by three.

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
`SettlementPaymentSucceeded→Complete`). Cancellation always refunds escrow. The `artistShare` figure
is computed by `IArtistShareCalculator` (keyed strategy — `DoorSplitCalculator` / `VersusCalculator`)
consumed by `PayoutFinishStep`. Payment webhooks return as integration events
(`PaymentSucceeded/FailedEvent`) handled by the `*Processor` classes, which route by
`Metadata["type"]` and drive the matching `IConcertWorkflowModule` method; idempotency via the inbox.

### 2.8 Ticket payee vs settlement payee

`TicketPayeeResolver` (`Application/Resolvers/TicketPayeeResolver.cs`, `ITicketPayeeResolver`) decides
who receives a concert's **ticket revenue**, via a `FrozenDictionary<DealType, IPayeeResolver>` (keyed
strategy): FlatFee/DoorSplit/Versus → `VenuePayeeResolver` (the venue is the box office); VenueHire →
`ArtistPayeeResolver` (the artist rents the room and keeps the gate).

`SettlementPayeeResolver` (`ISettlementPayeeResolver`) is its **exact inverse** — who receives the
*settlement/escrow release* (the DAC7 "seller"): FlatFee/DoorSplit/Versus → the artist, VenueHire →
the venue. The `FinishExecutor` DAC7 gate resolves the payee through it. Both facades reuse the same
`IPayeeResolver` leaves (the generic venue/artist party extractors); only the deal→party map differs.
Consumers never branch on deal type themselves.

---

## 3. The lifecycle entities

| Entity | Holds `LifecycleState`? | Role | TPH subtypes |
|---|---|---|---|
| `ApplicationEntity` | **Yes** (`State`, the only place) | The lifecycle owner | `StandardApplication`, `PrepaidApplication { PaymentMethodId }` (VenueHire) |
| `BookingEntity` | No | Links an accepted application to its concert | `StandardBooking`, `DeferredBooking { PaymentMethodId }` (DoorSplit/Versus) |
| `ConcertEntity` | No (`DatePosted` = draft vs posted) | The live concert | (single type) |
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

The single spot that ties a deal type to its lifecycle + steps + workflow is one `AddConcertWorkflow`
block. The executors, dispatchers, transitioner, factory, and registries are all deal-type-agnostic
(keyed DI + capability matching) and need no changes.

1. **`Deal.Contracts`** — add the case to `DealType.cs`; add an `XDeal : IDeal` record + a
   `[JsonDerivedType]` line on `IDeal.cs`.
2. **`Deal.Domain` / `.Application` / `.Infrastructure`** — add `XDealEntity : DealEntity` (typed
   columns + `Create`/`Update`/validator), an `XDealMapper`, an `XDealUpdater` wired into `DealUpdater`,
   and an EF config.
3. **Migrations** — re-scaffold: run `./initial-migrations.ps1` from `api/` (per `api/CLAUDE.md`; never
   an additive migration).
4. **Concert `Infrastructure/.../Steps/`** — reuse an existing step where the money shape fits
   (`SimpleApplyStep`, `PaidAcceptStep`, `CreateConcertDraftStep`, `RefundEscrowStep`, …); write a new
   concrete step only if the money movement is genuinely new.
5. **Concert `Infrastructure/.../Workflows/`** — add `XWorkflow : IConcertWorkflow, I{Applies…},
   I{Accepts…}` picking the capability interfaces that match its apply/accept/checkout shape.
6. **`AddConcertWorkflows()`** (`Concert.Infrastructure/Extensions/ServiceCollectionExtensions.cs`) —
   add the `services.AddConcertWorkflow(registryBuilder, DealType.X, p => p.WithApply<…>()…
   .WithFinish<…>(state).WithWorkflow<XWorkflow>())` block. This wires the state-machine edges, the
   keyed workflow, and the steps.
7. **Revenue/payee, only if new** — add a key to `TicketPayeeResolver` **and** `SettlementPayeeResolver`; if the deal pays a revenue share at
   Finish, add an `IArtistShareCalculator` strategy + key; add the matching
   `PaymentAmountMapper`/`TermsRenderer`/`TermsSerializer` variant (each a per-deal-type keyed family).
8. **Payment** — if the deal needs onboarding verification, register an `IStripeValidationStrategy`
   keyed by the new `DealType`.
9. **Frontend** — add the deal form + accept/apply checkout UI variant.

Re-using existing step impls is the main win of the capability-interface design.

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
| Step impls read typed properties | `Concert.Infrastructure/.../Steps/*Step.cs` | `PayoutFinishStep` reads `ArtistDoorPercent` via the calculator; a custom deal has no typed property — you'd need a rule interpreter or a finite set of rule kinds. |
| Stripe primitives are rigid | Payment | Connect exposes a small finite set of operations; custom deals still map onto that set. |
| `TicketPayeeResolver`/`SettlementPayeeResolver` hard-code direction | `Concert.Application/Resolvers/` | Who keeps ticket revenue and who receives settlement are tables keyed by `DealType`; a custom deal must declare both. |

### 5.2 Realistic options

- **Option A — keep the closed shape, make adding types cheaper.** Adding a developer-defined type is
  already largely mechanical (§4). QoL wins: move the share formula to a single home (§6.1), generate
  the `TicketPayeeResolver`/`SettlementPayeeResolver` maps from workflow metadata.
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
- **`IConcertWorkflowModule`** (in `Concert.Contracts`) is the thin cross-module facade — the
  payment-outcome callbacks (`VerifySucceededAsync`, `EscrowSucceededAsync`, `SettlementSucceededAsync`)
  plus `FinishAsync` / `CancelAsync`. Apply / Accept / Checkout are HTTP-only, called via dispatchers
  from `Concert.Api` controllers.
- **`ConcertWorkflowBuilder` runs at the composition root**, not per request; all workflows and state
  machines are wired once in `AddConcertWorkflows`.

### 6.1 Artist-share formula lives in two places

The share formula exists both on the domain entities (`DoorSplitDealEntity.CalculateArtistShare`,
`VersusDealEntity.CalculateArtistShare`) and in the runtime `IArtistShareCalculator` strategies
(`DoorSplitCalculator`, `VersusCalculator`) used by `PayoutFinishStep`. At runtime only the strategies
are used; the entity methods survive **only as the oracle in the integration tests**
(`ConcertDoorSplitApiTests`, `ConcertVersusApiTests`). Pick one home — or keep the entity method as the
deliberate independent test oracle and note that intent where it's defined.

### 6.2 `IDealStrategy` is under-used

It's a marker with a single extender (`IStripeValidationStrategy` in Payment). Either grow it into a
real cross-module extension surface (per-deal calculators, projections, validators) or drop it.
