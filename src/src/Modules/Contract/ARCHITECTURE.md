# Contract Architecture

How settlement contracts and the concert lifecycle workflow fit together. Read this
before touching `api/Modules/Concert/Concertable.Concert.Application/Workflow/`,
`api/Modules/Concert/Concertable.Concert.Infrastructure/Services/Workflow/`, or
`api/Modules/Contract/`.

See [`OVERVIEW.md`](./OVERVIEW.md) for the product-level summary of what a contract is.
This doc is about the *code*.

---

## TL;DR

There are two collaborating sub-systems:

1. **The Contract module** owns the *data* — what kind of contract, with what numbers,
   on which `PaymentMethod`. Shape per contract type is fixed at compile time via
   a TPH (table-per-hierarchy) entity model in `Concertable.Contract.Domain`.
2. **The Concert workflow** owns the *behaviour* — how an application progresses
   from `Applied → … → Finished` for that contract type, who pays whom, when Stripe
   gets called, and what each lifecycle stage does. Lives entirely in the Concert
   module (`api/Modules/Concert/`); the Contract module knows nothing about it.

A `ContractType` enum value is the strategy key that connects them.

```
                Apply         Checkout?     Accept       Verify?   Settle      Finish
  FlatFee       Simple        AcceptCO      Simple        -        NoOp        Release escrow
  DoorSplit     Simple        AcceptCO      Paid          Yes      Deferred    Off-session pay
  Versus        Simple        AcceptCO      Paid          Yes      Deferred    Off-session pay
  VenueHire     Paid          ApplyCO       Simple        -        NoOp        Release escrow
```

---

## 1. The Contract module

```
api/Modules/Contract/
├─ Concertable.Contract.Domain/Entities/
│  ├─ ContractEntity.cs                      (abstract TPH root)
│  ├─ FlatFeeContractEntity.cs               { Fee }
│  ├─ DoorSplitContractEntity.cs             { ArtistDoorPercent, CalculateArtistShare(rev) }
│  ├─ VenueHireContractEntity.cs             { HireFee }
│  └─ VersusContractEntity.cs                { Guarantee, ArtistDoorPercent, CalculateArtistShare(rev) }
├─ Concertable.Contract.Contracts/
│  ├─ ContractType.cs                        enum { FlatFee, DoorSplit, Versus, VenueHire }
│  ├─ PaymentMethod.cs                       enum { Cash, Transfer }
│  ├─ IContract.cs                           interface (+ [JsonDerivedType] for SPA wire)
│  ├─ FlatFeeContract.cs / DoorSplitContract.cs / …  records implementing IContract
│  ├─ IContractModule.cs                     facade (Get/Create/Update/Delete)
│  └─ IContractStrategy.cs                   empty marker for keyed strategies
└─ Concertable.Contract.Application/Services + Concertable.Contract.Infrastructure/…
```

Key invariants:

- **`ContractEntity`** is a TPH base with `Id`, `PaymentMethod`, abstract `ContractType`.
  Each subtype adds its own typed columns (`Fee`, `HireFee`, `ArtistDoorPercent`,
  `Guarantee`). Validation lives on the entity (`ValidateFee`, `ValidateArtistDoorPercent`).
- **`PaymentMethod`** (`Cash | Transfer`) is metadata for the off-platform settlement
  channel — it does **not** drive workflow timing. The dev seeders use it to label
  payouts, but no workflow code branches on it. (OVERVIEW.md's "decides *when* money
  moves" is a simplification — what actually decides "when" is which lifecycle stage
  a step is wired to.)
- **`IContractStrategy`** is currently only used to mark
  `IStripeValidationStrategy` in the Payment module (keyed-DI by `ContractType` —
  Account vs Customer onboarding rules per contract).
- **`Concert.Opportunity.ContractId`** is a satellite FK (no nav back, no SQL FK
  across the context boundary). Concert *reads* contracts through
  `IContractLoader` (request-scoped memoizer) which delegates to `IContractModule`.

There are 29 references to `ContractType.<value>` across the codebase. The enum is
load-bearing and assumed to be closed.

---

## 2. The Concert workflow

### 2.1 Lifecycle stage enum

`api/Modules/Concert/Concertable.Concert.Domain/Enums/ConcertStage.cs`:

```csharp
enum ConcertStage { None, Applied, Verified, Accepted, Settled, Finished }
```

`ConcertStage` is **global across all contracts**, but each contract walks a
**subset** of it. FlatFee for example skips `Verified` and `Settled` (NoOp). The
subset is declared implicitly by which steps you register for that contract — see
§2.5.

### 2.2 The three lifecycle entities

Three entities implement `ILifecycleEntity` (`Concert.Domain/ILifecycleEntity.cs`).
Each owns a slice of the stage range:

| Entity              | Stages it can hold                  | TPH subtypes                              |
|---------------------|-------------------------------------|-------------------------------------------|
| `ApplicationEntity` | Applied / Verified / Accepted       | `StandardApplication`, `PrepaidApplication { PaymentMethodId }` |
| `BookingEntity`     | Accepted / Settled                  | `StandardBooking`, `DeferredBooking { PaymentMethodId }` |
| `ConcertEntity`     | (after booking) → Finished          | (single type)                             |

The TPH split on Application/Booking exists so prepaid-at-apply (VenueHire) and
deferred-pay-at-finish (DoorSplit/Versus) can carry a `PaymentMethodId` without
nullable columns on the standard variants. See memory
`project_application_booking_tph.md`.

Each entity has a local `AdvanceStage(next)` guard that hard-codes which stages
the entity is allowed to transition through — these duplicate the per-contract
transition validator and are mostly belt-and-braces (see §6.1).

### 2.3 Step interfaces (`Workflow/Steps/`)

A **step** is the unit of contract-specific behaviour at one stage. Every step
implements `IConcertStep` which declares a static `Stage` (the `ConcertStage` it
lives at). Steps come in two flavours:

**Action steps** — perform the state change for a stage. One implementer per
contract, registered into the workflow:

- `IApplyCheckoutStep`       (stage `Applied`)  — returns a `Checkout` *before* applying
- `IAcceptCheckoutStep`      (stage `Accepted`) — returns a `Checkout` *before* accepting
- `ISimpleApplyStep`         (stage `Applied`)  — no payment-method input
- `IPaidApplyStep`           (stage `Applied`)  — captures `paymentMethodId`
- `ISimpleAcceptStep`        (stage `Accepted`)
- `IPaidAcceptStep`          (stage `Accepted`)
- `IVerifyStep`              (stage `Verified`)
- `ISettleStep`              (stage `Settled`)
- `IFinishStep`              (stage `Finished`)

**Concrete steps live in `Concert.Infrastructure/Services/Workflow/Steps/`.**
Most of the contract-specific money logic is in these files — e.g.
`DoorSplitFinishStep` reads the contract, computes `artistShare = rev * pct`,
and calls `IManagerPaymentModule.PayAsync(...)` off-session.

### 2.4 Capability interfaces (`Workflow/Capabilities/`)

A **capability** is the marker interface a workflow uses to advertise that it
*has* a given step. Dispatchers pattern-match on these:

```
IAppliesSimple { ISimpleApplyStep Apply }
IAppliesPaid   { IPaidApplyStep   Apply }
IAppliesCheckout { IApplyCheckoutStep ApplyCheckout }

IAcceptsSimple { ISimpleAcceptStep Accept }
IAcceptsPaid   { IPaidAcceptStep   Accept }
IAcceptsCheckout { IAcceptCheckoutStep AcceptCheckout }

IVerifies      { IVerifyStep Verify }
```

`ISettleStep` and `IFinishStep` live on `IConcertWorkflow` directly because every
workflow has them (Settle as `NoOpSettleStep` if the contract pays at-finish).

### 2.5 Workflow classes (`Workflow/Workflows/`)

A workflow class is a contract-specific bundle of steps that exposes the
capability matrix. Example (`FlatFeeWorkflow.cs`):

```csharp
internal sealed class FlatFeeWorkflow
    : IConcertWorkflow, IAppliesSimple, IAcceptsCheckout, IAcceptsSimple
{
    public FlatFeeWorkflow(
        SimpleApplyStep apply,
        FlatFeeAcceptCheckoutStep acceptCheckout,
        FlatFeeAcceptStep accept,
        NoOpSettleStep settle,
        FlatFeeFinishStep finish) { … }

    public ContractType Type => ContractType.FlatFee;
    public ISimpleApplyStep   Apply          => apply;
    public IAcceptCheckoutStep AcceptCheckout => acceptCheckout;
    public ISimpleAcceptStep  Accept         => accept;
    public ISettleStep        Settle         => settle;
    public IFinishStep        Finish         => finish;
}
```

There are four: `FlatFeeWorkflow`, `DoorSplitWorkflow`, `VenueHireWorkflow`,
`VersusWorkflow`. Each picks its own combination of capabilities.

### 2.6 Composition in DI (`Concert.Infrastructure/Extensions/ServiceCollectionExtensions.cs`)

`ConcertWorkflowBuilder` is a small fluent builder that:

1. Registers each step type as scoped (`services.AddScoped<TStep>()`).
2. Records the step's static `Stage` into a per-contract `stages` list.
3. Registers the workflow itself **keyed by `ContractType`** under
   `IConcertWorkflow` (`AddKeyedScoped<IConcertWorkflow, TWorkflow>(contractType)`).
4. On `.Build()`, materialises the per-contract transition sequence
   (`None → step.Stage[0] → step.Stage[1] → …`) and registers a keyed
   `IConcertTransitionValidator` for that contract.

Usage:

```csharp
workflowTypes[ContractType.FlatFee] = services.AddConcertWorkflow(
    ContractType.FlatFee, p => p
        .WithApply<SimpleApplyStep>()
        .WithCheckout<FlatFeeAcceptCheckoutStep>()
        .WithAccept<FlatFeeAcceptStep>()
        .WithSettle<NoOpSettleStep>()
        .WithFinish<FlatFeeFinishStep>()
        .WithWorkflow<FlatFeeWorkflow>());
```

The `workflowTypes` dictionary is also handed to
`ConcertWorkflowCapabilityRegistry` so callers can ask
`registry.Has<IAppliesPaid>(ContractType.VenueHire)` without resolving the workflow.

### 2.7 Dispatch path

The flow for any lifecycle transition is:

```
Controller / event handler
   ↓
IXDispatcher              (Concert.Infrastructure/Services/Workflow/Dispatchers/)
   ↓
IXExecutor                (Concert.Infrastructure/Services/Workflow/Executors/)
   ↓
IWorkflowStateMachine<TEntity>
   ├── ConcertTransitionValidator.CanTransitionTo(from, target)   ← per-contract
   ├── IConcertWorkflowFactory.Create(contractType)               ← keyed-DI lookup
   ├── pattern-match on capability interface (IAppliesPaid, …)
   ├── invoke the matched IXStep.ExecuteAsync(...)
   └── entity.AdvanceStage(target) + SaveChanges
```

Dispatch examples (see `Workflow/Executors/`):

- `ApplyExecutor`: `workflow switch { IAppliesPaid w when pm!=null => w.Apply…, IAppliesSimple w => w.Apply…, _ => throw }`
- `AcceptExecutor`: same pattern with `IAcceptsPaid` / `IAcceptsSimple`
- `CheckoutDispatcher`: `IAppliesCheckout` / `IAcceptsCheckout`
- `VerifyExecutor`: `IVerifies` (or throws — Verify is optional)
- `SettleExecutor` / `FinishExecutor`: every workflow has Settle+Finish, no switch needed

The cross-module entry point is `IConcertWorkflowModule` in `Concert.Contracts`
(Settle / Finish / Verify) — the rest of the dispatchers are called from
controllers in `Concert.Api`.

### 2.8 Money-movement coupling

Steps call into Payment via two facades from `Payment.Contracts`:

- `IManagerPaymentModule` — `CreateHoldSessionAsync`, `CreateSetupSessionAsync`,
  `CreateVerifySessionAsync`, `FindHeldIntentAsync`, `PayAsync` (off-session).
- `IEscrowModule` — `DepositAsync`, `CaptureAsync`, `ReleaseByBookingIdAsync`.

The step picks the right call based on the contract's economics:

| Contract  | At Accept                       | At Finish                              |
|-----------|----------------------------------|----------------------------------------|
| FlatFee   | `Escrow.CaptureAsync` (bind pre-auth) | `Escrow.ReleaseByBookingIdAsync`    |
| DoorSplit | (none — booking deferred)        | `ManagerPaymentModule.PayAsync` off-session |
| Versus    | (none — booking deferred)        | `ManagerPaymentModule.PayAsync` off-session |
| VenueHire | `Escrow.DepositAsync` (artist pays venue) | `Escrow.ReleaseByBookingIdAsync` |

Note that the artist-share formula is duplicated:

- Domain: `DoorSplitContractEntity.CalculateArtistShare(rev)`,
  `VersusContractEntity.CalculateArtistShare(rev)`
- Step: same formula re-written inline in `DoorSplitFinishStep` /
  `VersusFinishStep` (against the DTO records, not the entities)

Either remove the entity methods (unused at runtime) or move the calculation to a
shared helper the step calls.

### 2.9 Ticket payee

`TicketPayeeResolver` (`Workflow/TicketPayeeResolver.cs`) is the only other place
`ContractType` is hard-coded — it picks who receives ticket revenue:
VenueHire → artist; everything else → venue.

---

## 3. Adding a new contract type — current workflow

1. **Contract.Contracts**
   - Add `ContractType.MyNewType` to the enum.
   - Add `[JsonDerivedType(typeof(MyNewTypeContract), "myNewType")]` to `IContract`.
   - Add `MyNewTypeContract : IContract` record with the contract's fields.
2. **Contract.Domain**
   - Add `MyNewTypeContractEntity : ContractEntity` with the typed columns
     and a `Create`/`Update`/validator.
3. **Contract.Infrastructure**
   - Add an `IEntityTypeConfiguration<MyNewTypeContractEntity>` and a
     `MyNewTypeContractUpdater`. Wire the updater into `ContractUpdater`.
4. **Migrations** — run `./initial-migrations.ps1` from `api/` (per `CLAUDE.md`).
5. **Concert.Infrastructure/Services/Workflow/Steps/** — write whatever new
   step impls you need (or reuse existing ones — `SimpleApplyStep`,
   `PaidAcceptStep`, `NoOpSettleStep`, `DeferredVerifyStep` are deliberately
   contract-agnostic and reusable).
6. **Concert.Infrastructure/Services/Workflow/Workflows/** — add
   `MyNewTypeWorkflow` implementing `IConcertWorkflow` plus whichever
   capability interfaces apply.
7. **`AddConcertModule`** — add the `services.AddConcertWorkflow(ContractType.MyNewType, p => p.With…())` block.
8. **Payment** — if your contract needs onboarding verification, register an
   `IStripeValidationStrategy` keyed by your `ContractType`.
9. **TicketPayeeResolver** — add a row to its frozen dictionary.
10. **Frontend** — add a contract form + accept/apply checkout UI variant.

Re-using existing step impls (steps 5–6) is the main win of the capability-
interface design — `SimpleApplyStep` for example is reused by FlatFee, DoorSplit,
and Versus with zero contract-specific code.

---

## 4. Can this support custom / drag-and-drop contracts?

**Short answer:** not in its current shape. But the workflow scaffold is closer
than it looks — the blocker is the *data* side, not the *behaviour* side.

### 4.1 What stands in the way of a fully user-defined contract

| Concern | Where it lives | Why it blocks dynamic contracts |
|---------|----------------|----------------------------------|
| `ContractType` is a closed enum | `Contract.Contracts/ContractType.cs` | Every keyed-DI lookup, every switch, every JSON polymorphic discriminator assumes a finite set known at compile time. User-defined contracts would need an open identifier (string/Guid) and runtime registration. |
| TPH schema per subtype | `Contract.Domain/Entities/*ContractEntity.cs` + EF configs | Each contract type currently gets its own columns. A user-defined contract has unknown shape at migration time — has to be a JSON blob, a generic key-value table, or a rule list. |
| Step impls are typed code | `Workflow/Steps/*Step.cs` | `DoorSplitFinishStep` reads `contract.ArtistDoorPercent` directly. A custom contract has no typed property to read — you'd need an expression interpreter (`rev * <pct field>`) or a finite set of "rule kinds" (FlatCharge, PercentSplit, Hold, Release) the step iterates over. |
| Stripe primitives are rigid | `Payment.Infrastructure/Services/` | Connect has a small finite set of operations (PaymentIntent on/off-session, SetupIntent, Transfer, Refund). Custom contracts still ultimately map to that finite set — the interpreter doesn't get to invent payment flows. |
| `ConcertStage` is a closed enum | `Concert.Domain/Enums/ConcertStage.cs` + `AdvanceStage` guards on the three lifecycle entities | A "drag your own stages" UX would need stages to be open values (string/record-struct). See §6.1. |
| `TicketPayeeResolver` hard-codes direction | `Workflow/TicketPayeeResolver.cs` | Who-pays-whom for ticket revenue is a table keyed by `ContractType`. A custom contract would need to declare its payee explicitly. |

### 4.2 The realistic options

**Option A — Keep the closed shape, make adding new types cheaper.**
The current architecture is already pretty composable for *developer-defined*
contract types. The work to add a new one is largely mechanical (§3). Quality-of-life
wins worth taking even without going dynamic:

- Open up `ConcertStage` per §6.1 so workflow-private stages don't bloat the
  shared enum.
- Move the artist-share formula off the steps onto the domain entity (the methods
  already exist; they're just unused).
- Generate the `TicketPayeeResolver` dictionary from a method/attribute on the
  workflow rather than a separate file.

**Option B — Add a single `Composite` contract type that's user-configurable.**
This is the pragmatic middle path and the one I'd recommend if drag-and-drop is
the goal. You add **one** new `ContractType.Composite` (or `Custom`) with:

- A `CompositeContractEntity : ContractEntity` whose single column is a JSON
  document: a *contract template* describing a list of `Rule`s (each rule has
  a kind, amount expression, payer/payee, trigger stage).
- A `CompositeContract : IContract` record exposing the parsed template to
  the SPA.
- A `CompositeWorkflow` whose steps **interpret** the template:
  `CompositeAcceptStep` iterates rules with trigger `Accepted` and invokes
  the matching escrow/payment primitive; `CompositeFinishStep` does the same
  for `Finished`; etc.
- A finite vocabulary of `Rule` kinds: `FlatCharge`, `PercentSplit`,
  `Guarantee`, `Hold`, `Release`, `Refund`. The SPA's drag-and-drop palette is
  exactly this vocabulary.

This approach:
- Keeps the rest of the system entirely unchanged. The four existing contract
  types stay as-is.
- Doesn't require migrations per user contract (one JSON column).
- Lets you build the UI incrementally — start with one rule kind, add more.
- Stripe primitives map cleanly to rule kinds, so the interpreter has a finite
  switch instead of an open language.
- The four built-ins (FlatFee, DoorSplit, Versus, VenueHire) can eventually be
  re-expressed as preset templates, and the typed entities deprecated. But you
  don't have to do that day one.

**Option C — Open the `ContractType` identifier entirely.**
Replace the enum with a `ContractTypeId` value type (string or Guid) backed by
a `ContractTemplate` table, runtime DI registration, and a generic workflow
factory. Workable but invasive — touches 17+ files and breaks the JSON polymorphic
discriminator. Only worth doing if Option B's single-composite model proves too
restrictive.

### 4.3 Recommendation

If the product question is "can a venue/artist drag-and-drop their own contract
terms": **build Option B as a new contract type**. The capability-interface +
workflow-builder pattern accommodates it cleanly — `CompositeWorkflow` is just
one more workflow class, and the interpreter lives in its steps. Almost all of
the existing scaffolding is reusable; the new work is the rule schema, the
interpreter, and the SPA builder UI.

If the product question is "I want to keep adding hard-coded contract types
without it feeling so heavyweight": Option A is what to do, and §6 lists the
specific files to touch.

---

## 5. Frequently confused things

- **`PaymentMethod` ≠ `paymentMethodId`.** `PaymentMethod` is the contract-domain
  enum (`Cash | Transfer`) used for accounting. `paymentMethodId` is a Stripe
  PM id (`pm_…`) flowed through `PaidApplyStep` / `PaidAcceptStep` /
  `DeferredBooking`. Different things.
- **`ConcertWorkflowBuilder` is at the composition root, not at runtime.** All
  workflows are wired once in `AddConcertModule`. There is no per-request
  workflow construction.
- **`IConcertWorkflowModule` (in `Concert.Contracts`) is a thin facade**: only
  Settle / Finish / Verify. Apply / Accept / Checkout are HTTP-only and called
  directly via dispatchers from `Concert.Api` controllers.
- **`IContractStrategy` is currently a near-empty marker.** Only
  `IStripeValidationStrategy` extends it. Not a general extension point yet.
- **The `Standard` vs `Prepaid` Application split and `Standard` vs `Deferred`
  Booking split are about *carrying a `PaymentMethodId`*, not about workflow
  branching.** Workflow branching is the capability interfaces; the TPH split
  is just so we don't have nullable PM-id columns on rows that don't use them.

---

## 6. Open issues / future work

### 6.1 Stage-enum bloat for workflow-private stages

Today, declaring a new step `FooStep` for `FooWorkflow` only that lives at a
*new* lifecycle stage means:

- Add `Foo` to `ConcertStage` enum.
- Update the `AdvanceStage` guards on whichever of `ApplicationEntity` /
  `BookingEntity` / `ConcertEntity` owns that stage.

The per-contract transition validator (built from registered steps' `Stage`s) is
already isolated — other contracts' validators won't include `Foo`. The bloat is
purely in the global enum file and the entity guards.

Two cleanups:

1. **Replace `enum ConcertStage` with `readonly record struct ConcertStage(string Name)`**
   (or a sealed class with static singletons). Each workflow declares its own
   stages by name; the validator's sequence-comparison logic is unchanged.
   Storage becomes `string`/`int`-hash on the DB column.
2. **Remove the per-entity `AdvanceStage` guards.** They duplicate
   `ConcertTransitionValidator` (which the state machine already calls) and are
   the only reason the entities need to know about every stage. Once the
   validator is the single source of truth, adding a stage touches only the
   workflow that uses it.

Combined, this would make adding `FooStep`+`Foo` stage a single-file change
inside the Foo workflow registration.

### 6.2 Artist-share formula duplication

`DoorSplitContractEntity.CalculateArtistShare` and
`VersusContractEntity.CalculateArtistShare` exist on the entities but aren't
called — the finish steps re-implement the same formulas against the DTO
records. Pick one home.

### 6.3 `IContractStrategy` is under-used

It's a marker but only used to constrain Stripe validation strategies. Either
expand it into a real cross-module extension surface (per-contract calculators,
projections, validators) or drop it.

### 6.4 OVERVIEW.md drift

OVERVIEW.md says `PaymentMethod` "decides *when* money moves through Stripe."
What actually decides "when" is which lifecycle stage a step is wired to.
`PaymentMethod` is just settlement-channel metadata. Worth a one-line fix.
