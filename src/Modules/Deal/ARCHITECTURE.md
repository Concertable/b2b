# Deal and lifecycle architecture

How the **deal** data and the **per-stage lifecycle** fit together. Read this before touching
`api/.../Modules/Deal/` or any of the `Application`, `Booking`, and `Concert` modules' `Domain/Lifecycle/`
and Deal-strategy code. Section 5 is the configurable-product target. The detailed four-case walkthrough
in §§1–4 retains historical symbol examples; refresh concrete interfaces against the owning module when
changing them, rather than recreating a superseded mapper or lifecycle contract.

Two names that are easy to confuse, and that a past refactor deliberately separated:

- **Deal** — the *economic arrangement* (flat fee / door split / versus / venue hire), with its
  numbers (`Fee`, `HireFee`, `ArtistDoorPercent`, `Guarantee`) and its `PaymentMethod`. It is the
  editable current offer. Lives in the **Deal module** (`Modules/Deal/`), keyed by the `DealType` enum.
- **Contract** — the *signed binding artifact* (parties + both e-signatures + rendered legal terms +
  PDF), a frozen by-value snapshot formed at Accept. It is the `ContractEntity` in the **Booking
  module**, and is a different thing from the Deal it was rendered from.

---

## TL;DR

Separate economic data from module-owned lifecycle behaviour:

1. **The Deal module** owns the *data* — what kind of deal, with what numbers, on which
   `PaymentMethod`. The current four shapes use TPT (table-per-type) persistence in
   `Concertable.B2B.Deal.Domain`; §5 replaces whole-Deal subtype identity with configurations.
2. **Application, Booking and Concert** own their respective lifecycle behaviour and fixed stage order.
   Each stage owns its operations and capability implementations. Deal does not orchestrate the journey;
   Payment remains unaware of B2B deal types.

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
  `IDealStrategy`, requiring exactly one case for every deal type. No family needs that escalation today:
  Application's `IApplyStep` and `ICommitmentReferenceStep` are same-interface families behind
  `IDealStrategyFactory<TStrategy>`. A family fractures when a caller cannot supply the shared header
  without a placeholder — apply checkout, which runs before the application row exists and so holds no
  application id, is the first path that would. `DealUnionBuilder` stays for it.

Concert's homogeneous Cancel/Complete steps (`ICancelStep`, `ICompleteStep`, under
`Strategies/Steps/<Operation>`) use the shared Deal step factory. `IKeyedServiceProvider` never escapes
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

Every Payment operation is named by a `PaymentOperationReference` minted by
`PaymentOperationReferences` (`Concertable.B2B.Infrastructure/Payments`): an operation type plus the
`app:`/`booking:`/`concert:`-prefixed id B2B itself encoded. Nothing provider-shaped crosses the
boundary — B2B stores the reference and the opaque operation id Payment returns, and no Stripe
identifier at all.

Checkout opens a session through **`IPaymentSessionOperationsClient`** (`SetupPaymentMethodAsync`,
`CreateAsync`). The escrow moves Payment must own and retry go over the bus as commands
(`CaptureEscrowCommand`, `DepositEscrowCommand`, `RefundEscrowCommand`); the finish-step moves call
**`IEscrowOperationsClient.ReleaseAsync`** and **`ISettlementOperationsClient.PayAsync`** directly.
Amounts flow tenant → tenant, sourced from the frozen tenant snapshot on the application/booking.

| Deal | Checkout session | Accept-step money | Finish-step money |
|---|---|---|---|
| **FlatFee**   | accept-time `CreateAsync`: `Authorization`, `EscrowHold(applicationId)`, destination-routed — venue pre-auth of `deal.Fee` | `FlatFeeConfirmStep`: `CaptureEscrowCommand` (venue→artist) into escrow | `ReleaseEscrowCompleteStep`: `ReleaseAsync` → artist |
| **VenueHire** | apply-time `SetupPaymentMethodAsync`: `PaymentMethodSetup`, `MethodSetup(opportunityId, artistTenantId)` — artist mandate for `deal.HireFee` | `VenueHireConfirmStep`: `DepositEscrowCommand` (artist→venue) into escrow off-session | `ReleaseEscrowCompleteStep`: `ReleaseAsync` → venue |
| **DoorSplit** | accept-time `SetupPaymentMethodAsync`: `PaymentMethodVerification`, `MethodVerification(applicationId)` — venue card verify | `VerifiedConfirmStep`: no charge; the verified method is the contract's commitment | `PayoutCompleteStep`: off-session `PayAsync` (venue→artist), `artistShare = rev × ArtistDoorPercent` |
| **Versus**    | as DoorSplit | `VerifiedConfirmStep`: no charge | `PayoutCompleteStep`: off-session `PayAsync`, `artistShare = Guarantee + rev × ArtistDoorPercent` |

Escrow deals (FlatFee, VenueHire) confirm money **at Accept** and release **at Finish**
(`Booked +Finish→Complete`). Payout deals (DoorSplit, Versus) ring-fence nothing at Accept (verify +
store the mandate) and pay off-session **at Finish** (`Booked +Finish→AwaitingSettlement`, then
`SettlementPaymentSucceeded→Complete`). Escrow deals refund on cancellation — `EscrowCancelStep` in
Booking, `RefundEscrowCancelStep` in Concert — while payout deals cancel outright. Settlement gross comes
from `ISettlementAmountResolver`, whose named facade selects one keyed strategy through
`IConcertDealStrategyFactory`. The DoorSplit and Versus leaves share the revenue-loading base that
reads ticket revenue plus declared door revenue, then apply their own percentage-only or
guarantee-plus-percentage formula. `PayoutCompleteStep` and `InvoiceIssuer` consume that same facade so
charged and invoiced amounts cannot diverge. Payment reports each outcome as an integration event
carrying that operation's reference — the escrow commands through their own `*Succeeded`/`*Rejected`
events, a session-opened operation through `PaymentSucceeded/FailedEvent` — and the `*Processor`
classes filter on `Reference.OperationType` and read back the id they encoded; idempotency is
provided by the inbox.

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
| `ApplicationEntity` | **Yes** — `Application.Lifecycle.ApplicationState` | Terminal after its own decision (accept/reject/withdraw) | (single type) |
| `BookingEntity` | **Yes** — `Booking.Lifecycle.BookingState` | Owns confirmation, failure/retry, and pre-Concert cancellation | (single type) |
| `ConcertEntity` | **Yes** — `Concert.Lifecycle.ConcertState` | The live concert: draft/post, cancellation, settlement, completion | (single type) |
| `ContractEntity` | No | The signed binding artifact (see below) | (single type) |

FK chain: `OpportunityEntity (1)→(N) ApplicationEntity (1)→(0..1) BookingEntity (1)→(0..1)
ConcertEntity`, and `BookingEntity (1)→(0..1) ContractEntity`. `OpportunityEntity` is `ITenantScoped`
(the venue) and holds the satellite `DealId` FK into the Deal module.

Application and Booking are each a single type: the commitment that once justified a TPH split is a
`PaymentOperationReference` frozen onto `ContractEntity`, so neither aggregate carries a payment column.

**`ContractEntity`** (`Booking.Domain/Entities/ContractEntity.cs`) is a by-value immutable snapshot
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

## 4. Adding a new deal type in the current four-case model

This section describes the developer-defined subtype approach. It is not the extension mechanism for
the configurable product in §5; that target makes the current four arrangements supplied presets.

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

## 5. Configurable deals — target design

The four-case relational model is appropriate for the four current products. Configurable deals change
that requirement: whole deals become data assembled from a finite, code-owned economic and capability
language. The target is one versioned template/configuration model, not four permanent built-ins plus a
`Composite` exception. Current persistence uses TPT (`UseTptMappingStrategy`), not a single TPH table.
The limitation is per-deal subtype identity/columns and typed subtype reads, not relational storage
itself or module-local capability composition.

This is a target design, not a claim that configuration storage or a builder is already implemented.
The MVP still exposes the four existing arrangements through normal forms. The representation refactor
is not an additional launch gate; tenant authoring, tenant-specific entitlements and a visual builder
are separate future product features.

### 5.1 Domain and lifetime

| Concept | Meaning |
|---|---|
| `DealTemplate` | Reusable blueprint with typed parameters/defaults and permitted rule/capability structure; stable identity, editable draft, immutable published revisions |
| `DealConfiguration` | Reusable specialization pinned to one template revision, selecting capabilities and fixed/default terms plus allowed per-offer inputs; separately versioned |
| `Deal` | Concrete editable offer, pinned to a configuration revision, with offer-specific values bound; edits create immutable offer revisions under a concurrency-controlled head |
| `DealTerms` | Immutable typed economic value/graph, with no independent identity or workflow; future settlement facts are explicit typed input references, not unspecified agreed terms |
| `Contract` | Booking-owned immutable accepted snapshot: complete effective terms, selected capabilities/semantic versions, parties, provenance and signing artifacts |

Templates/configurations start as explicitly platform-owned presets. Future tenant-owned specializations
can narrow parameters or select allowed capabilities without mutating a shared template or cloning every
preset for every tenant. Drafts are editable; publishing freezes a revision. New revisions never change
existing references. A Contract contains the resolved snapshot, not only IDs pointing to a configuration
that must be looked up later. Archival controls new use, not historical interpretation.

### 5.2 Hybrid PostgreSQL persistence

Keep identity, owner scope/tenant, status, revision numbers, provenance, concurrency tokens and selected
capability declarations relational. Store the strongly typed term/rule graph in `jsonb` on immutable
revision rows. Capability children and the graph form one validated aggregate committed atomically.
The Contract's frozen document materializes the entire effective graph and capability selection.

A single JSON document with a relational envelope is viable but makes capability relationships harder
to constrain/query. Fully normalized rule/parameter tables help some reports but add graph joins and
either subtype-table churn or weakly typed EAV parameters; they still need semantic validation.
Keeping only a `Composite` JSON escape hatch creates parallel execution, validation, snapshot and
reporting models for equivalent arrangements. None justifies permanent whole-Deal subclasses.

Use relational keys/uniqueness and row-local JSON type/range checks, with required fields and SQL-null
behaviour made explicit. These constraints cannot prove arbitrary graph semantics or deployed handler
compatibility. Enforce revision and capability-child immutability at the database write boundary.
Use ordinary indexes for tenant/status/identity/revision/capability queries, selective GIN for relevant
containment/path queries, and expression indexes or typed read projections for financial ranges and
analytics. Derived projections never become a second editable source of terms. Actual money reporting
continues to use recorded settlement, invoice and Payment ledger facts.

### 5.3 Finite typed rules and capability compatibility

The language has closed C# alternatives with typed parameters, not arbitrary user-authored code.
Concepts such as FlatCharge, PercentSplit and Guarantee express economics; Hold, Release and Refund
refer to supported effects and their module-owned capabilities. No scripts, free-form executable
expressions, CLR type names, runtime DI registrations, unbounded loops or arbitrary network calls are
part of a configuration.

A server write-boundary compiler rejects unknown kinds/versions, invalid types/ranges/currency/roles,
unbounded or cyclic graphs, unavailable phase inputs and incompatible capabilities. Validate the
whole graph and its ordering/dependencies, not only pairwise allowlists. A release requiring held funds
must be backed by the allowed funding path; no template can expand the code-owned capability language.
Incomplete drafts are not executable. Publication, bound offer creation and acceptance require validated
values; execution additionally checks current permissions, balances and provider/lifecycle state.

The runtime catalog is finite and composition-tested; configuration IDs are open data identifiers,
never DI keys. Multiple configurations can select the same operational capability. A new supported
combination needs data publication, not a new Deal enum case, migration or deployment. New rule/effect
semantics require code deployment; relational schema/index changes can still need migrations.

### 5.4 Reproducibility, acceptance and concurrency

Separate business revision, document schema version and rule/capability semantic version. Preserve
versioned readers/evaluators and recorded settlement inputs/results for historical agreements.
Configuration migrations produce new validated revisions; they never rewrite a signed agreement.
Hash a versioned canonical semantic payload and separately retain exact signed text/PDF bytes: `jsonb`
does not preserve original JSON bytes or key order.

Acceptance atomically verifies the specific offered revision and signature bindings, creates the
Contract snapshot and advances the owning lifecycle within the established transaction/idempotency
boundary. Later offer edits cannot change the agreement. Currency, rounding, party direction and future
revenue-input definitions are part of the frozen economics, not ambient configuration.

An explicit aggregate `bigint` edit token guards mutable heads/drafts. Every child selection update
participates in the same compare-and-swap transaction. Immutable revision IDs are not edit tokens.

### 5.5 Behaviour and future tenant access

Application, Booking and Concert retain their fixed stage order and independent state machines.
Configuration selects approved capabilities within module-owned extension points; it does not own an
end-to-end workflow or permit arbitrary stage reordering. Factories dispatch on validated finite
capability kind/version. Keep genuine same-interface families and operation-owned honest-header unions
where required; uniform operations stay uniform. Do not manufacture per-preset strategies or restore
an obsolete interface merely because earlier examples named it.

B2B retains one authority for deal-gross calculations, shared with payout and invoicing. Payment stays
deal-agnostic, owns commission bindings and moves money through its existing primitives. The four current
formulas are equivalence fixtures, including Versus as guarantee **plus** percentage share.

Economic compatibility, tenant eligibility, actor permissions and row visibility are separate concerns.
Future tenant restrictions can use existing active-tenant/membership authorization at author/publish/use
boundaries; they must also inspect the selected capabilities, not just template IDs. Private libraries
and counterparty-visible offers need their own authorized read surfaces. Later entitlement revocation
does not rewrite Contracts or remove the ability to complete/refund existing obligations. This does not
require Finbuckle; replacing tenant resolution/options/isolation infrastructure is a separate decision.

---

## 6. Frequently confused things & open issues

- **`Deal` ≠ `Contract`.** The Deal is the editable economic offer (Deal module); the `ContractEntity`
  is the frozen signed artifact formed at Accept (Booking module). Different lifetimes, different
  models.
- **`PaymentMethod` ≠ the payment commitment.** `PaymentMethod` is the Deal-domain enum
  (`Cash | Transfer`) used for accounting; the commitment is the `PaymentOperationReference` B2B mints and
  freezes onto the contract, which Payment resolves to a provider object it never shows B2B.
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
