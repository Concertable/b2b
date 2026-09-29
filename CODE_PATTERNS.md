# B2B — structural rosters

B2B's own precedents for two patterns whose generic shape lives in the `multitenancy` and
`keyed-strategies` skills. Read those first; this file is only the roster of real types, which they
deliberately omit. Nothing here restates a rule.

## The DbContext stances, per module

The bases live in `B2B.DataAccess.Infrastructure`; each concrete context lives in its own module's
`Infrastructure/Data/`. Each composes the module's anemic `XConfigurationProvider`; none modifies it.

| Stance | Base | Concrete examples |
|---|---|---|
| Tenant-filtered single owner | `TenantScopedDbContext` | `VenueDbContext`, `ArtistDbContext`, `OpportunityDbContext` |
| Resource audience and grants | `ResourceScopedDbContext` | `ApplicationDbContext`, `BookingDbContext`, `ConcertDbContext`, `ConversationsDbContext` |
| Tenant-independent read, `SaveChanges` throws | `ReadDbContext` (shared DataAccess) | `Application`, `Artist`, `Booking`, `Concert`, `Opportunity`, `Venue` |
| Unscoped but writable | `PrivilegedDbContext` | `ArtistPrivilegedDbContext`, `OpportunityPrivilegedDbContext`, `ConversationsPrivilegedDbContext` |
| Module-owned configuration | `DbContextBase` + own `OnModelCreating` | `Admin`, `Deal`, `Tenant`, `User` |

Each scoped context declares its filters in `ApplyTenantFilters`. Artist, Venue and Opportunity use
`ApplySingleOwner` for their tenant-owned entities. Application, Booking, Concert and Conversations use
resource grants and audiences for shared visibility; their ordinary contexts inherit `ResourceScopedDbContext`.

Repository examples distinguish visibility from exposed operations: `ArtistRepository` is tenant-bound,
`ArtistReadRepository` uses `ArtistReadDbContext`, and `ArtistPrivilegedReadRepository` exposes
transaction-enlisted queries through `ArtistPrivilegedDbContext`. The latter's read-only contract keeps
`Read` even though its implementation context is writable. `ConcertPrivilegedRepository` exposes both
reads and writes through its privileged context. The generic capability/visibility naming rules belong
to `persistence` and `multitenancy` respectively.

A service holding both `repository` and `readRepository` uses those fields for the two stances of its own
aggregate. The domain capability `IConcertAvailability` has its own purpose-named abstraction over the
read context.

## Owned child collections without their own repository

The `persistence` skill's "one repository per entity" rule has one sanctioned exception: an entity that is
always read or written jointly with one owning aggregate, and never queried independently, stays a `DbSet` on
the owning repository rather than gaining a repository of its own. `ConcertImageEntity` is the current
example. `ConversationReadPosition` has its own `ConversationReadPositionRepository`, which owns read-position
advancement. The moment a consumer needs an owned child independently of its owner, that need earns it a
real repository.

## Which entities are filtered

- **Owner-filtered:** `Venue`, `Artist`, `Opportunity` in their ordinary tenant contexts; marketplace
  browsing uses the separate read stance.
- **Resource-filtered:** Application, Booking/Contract, Concert/Invoice and Conversations use live grants
  and the audience required by the operation. Public Concert listing uses its read stance.
- **Module-owned, unfiltered context:** Deal; transaction-enlisted cross-module reads use
  `DealPrivilegedReadRepository`.

## The `DealType` strategy families

Declared vertically at each owning module's composition root through `DealStrategyBuilder`, then resolved
through the shared scoped `IDealStrategyFactory<TStrategy>`. Named facades remain the business API:
`DealMapper`, `DealUpdater`, `DealTermsRenderer`, and `SettlementAmountResolver`.

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
These are current-code rosters. The [configurable Deal target](./api/src/Modules/Deal/ARCHITECTURE.md#5-configurable-deals--target-design)
describes the future representation and capability-selection boundary.

## Capability, not `DealType`

The concerns partition the four types differently, so no one hierarchy serves them all:

| Concern | Types |
|---|---|
| Door revenue drives settlement | DoorSplit, Versus |
| `FinancialOperation` raised at confirmation | FlatFee (capture), VenueHire (deposit), DoorSplit + Versus (verify) |
| Payment commitment minted at checkout | FlatFee (authorization hold), VenueHire (method setup), DoorSplit + Versus (method verification) |
| Supply direction reverses ([`LEGAL_REQUIREMENTS.md`](./api/src/Modules/Deal/LEGAL_REQUIREMENTS.md)) | VenueHire |

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
