# Deal strategies, unions and lifecycle capabilities

[Pattern index](../CODE_PATTERNS.md). Generic mechanisms belong to `keyed-strategies` and `keyed-unions`.

## The `DealType` strategy families

Declared vertically at each owning module's composition root through `DealStrategyBuilder`, then resolved
through the shared scoped `IDealStrategyFactory<TStrategy>`. Named facades remain the business API:
`DealMapper`, `DealUpdater`, and `SettlementAmountResolver`. Terms rendering belongs to
`DealTerms.Render()` in Deal.Contracts.

The Deal-specific builder composes `KeyedStrategyBuilder<DealType>` and makes complete `DealType` coverage
innate for every registered strategy family. Adding a `DealType` member therefore fails composition until
every family handles it. `DealStrategyArchitectureTests` guards the shape.

## The workflow operations a `DealType` selects

Application, Booking and Concert each own one module-local workflow whose methods are the named lifecycle
operations for that stage. A workflow spans no module boundary and holds no aggregate state. Deal-varying
lifecycle work sits behind operation-named `*Step` interfaces resolved through
`IDealStrategyFactory<TStrategy>`: `IApplyStep` and `ICommitmentReferenceStep` (Application),
`IConfirmStep`/`ICancelStep` (Booking), and `ICancelStep`/`ICompleteStep` (Concert).
`IContractFactory` remains a non-step strategy resolved through `IDealStrategyFactory<TStrategy>`.

## The `DealType` unions

Where the variation is data rather than injected behaviour, `DealType` selects a type, not a strategy.

| Union | Arms | Role |
|---|---|---|
| `DealEntity` | `FlatFeeDealEntity`, `DoorSplitDealEntity`, `VersusDealEntity`, `VenueHireDealEntity` | the current editable offer; TPT, each leaf overriding `DealType` |
| `ConfirmedBookingTerms` | `FlatFee`, `VenueHire`, `DoorSplit`, `Versus` | the frozen economics carried on `ConfirmedBookingSnapshot` across the Booking→Concert seam |

`AcceptedApplication` is deliberately *not* a union: once Payment owned the payment-method commitment the
Accept arms became identical, so it is one record carrying the immutable `ApplicationAcceptanceSnapshot`.

`BookingEntity` is also a single sealed type; there are no `Standard`/`Deferred` entity arms.
These are current-code rosters. The [configurable Deal target](../api/src/Modules/Deal/ARCHITECTURE.md#5-configurable-deals--target-design)
describes the future representation and capability-selection boundary.

## Capability, not `DealType`

The concerns partition the four types differently, so no one hierarchy serves them all:

| Concern | Types |
|---|---|
| Door revenue drives settlement | DoorSplit, Versus |
| `FinancialOperation` raised at confirmation | FlatFee (capture), VenueHire (deposit), DoorSplit + Versus (verify) |
| Payment commitment minted at checkout | FlatFee (authorization hold), VenueHire (method setup), DoorSplit + Versus (method verification) |
| Supply direction reverses ([`LEGAL_REQUIREMENTS.md`](../api/src/Modules/Deal/LEGAL_REQUIREMENTS.md)) | VenueHire |

Split an interface on the capability a row names, never on the deal type holding it.

Which mechanism a varying input earns:

| The input is | Mechanism |
|---|---|
| stored in the deal's terms | keyed strategy family (`IDealStrategyFactory<TStrategy>`) |
| chosen by the user during one shared action | capability-keyed union over a tagged request union (`IDealUnionFactory<TUnion>`) |
| negotiated as its own act | its own endpoint |

`KeyedUnionBuilder`, `DealUnionBuilder` and `IDealUnionFactory<TUnion>` are the retained typed-escalation
tier. They currently have no lifecycle consumer — `Apply` and `Accept` both collapsed to keyed families once
the payment-method input left B2B — and are kept for the next capability whose shared action genuinely
fractures on legitimate client input.
