# B2B — structural rosters

B2B's own precedents for two patterns whose generic shape lives in the `multitenancy` and
`keyed-strategies` skills. Read those first; this file is only the roster of real types, which they
deliberately omit. Nothing here restates a rule.

## The DbContext stances, per module

The bases live in `B2B.DataAccess.Infrastructure`; each concrete context lives in its own module's
`Infrastructure/Data/`. Each composes the module's anemic `XConfigurationProvider`; none modifies it.

| Stance | Base | Concrete examples |
|---|---|---|
| Grant-reached — a tenant holding a live grant on the row | `ResourceScopedDbContext` | `ApplicationDbContext`, `BookingDbContext`, `ConcertDbContext`, `ConversationsDbContext` |
| Single-owner filtered — the row names its one owning tenant | `TenantScopedDbContext` | `VenueDbContext` (filters `Venue`/`VenueImage`), `ArtistDbContext` |
| Tenant-independent read, `SaveChanges` throws | `ReadDbContext` (shared DataAccess) | `Application`, `Artist`, `Booking`, `Concert`, `Opportunity`, `Venue` |
| Unscoped but writable | `PrivilegedDbContext` | `ConversationsPrivilegedDbContext` (moderation) |
| Untenanted module | `DbContextBase` + own `OnModelCreating` | `Admin`, `Deal`, `Tenant`, `User` — no base owns their `OnModelCreating`; `api/TECH_DEBT.md` holds the repo-wide entry |

`ResourceScopedDbContext` derives from `TenantScopedDbContext`, so a context can declare both stances: Concert
filters its concerts and invoices by grant and its self-billing agreements by single owner.

Filters are declared per entity in the owning context's `ApplyTenantFilters`, never auto-derived from a
marker. A single-owner entity uses `modelBuilder.ApplySingleOwner<TEntity>(this)`. A grant-reached entity
writes its predicate out against that context's own grant set, because the predicate has to name the grant
family it reads; the same predicate re-checks `MembershipAuthority` at the revision the request resolved.
`ResourceAccessGuardTests` fails a grant family whose configuration is unregistered, and a grant-scoped
context that declares no filter at all.

Query classes split by stance: `XRepository` (tenant-bound), `XReadRepository` (`XReadDbContext`),
`XPrivilegedRepository` (writable `PrivilegedDbContext`, only where a cross-tenant write flow exists, e.g.
`MessagePrivilegedRepository`, `ContentReportPrivilegedRepository`). A service holding both `repository` and `readRepository` is the convention when it
injects both stances of its own aggregate. A domain fact that is not naturally an entity repository may get
its own purpose-named abstraction over the read context — `IConcertAvailability`.

## Which entities are filtered

- **Unfiltered by design:** `Opportunity` (the applying artist reads the venue's opportunity to stamp the
  deal), `Deal` (the applying artist reads the venue's terms), `ConcertAvailability` (it answers only that a
  date is taken).
- **Grant-reached:** `Application`, `Booking`, `Contract`, `Concert`, `Invoice`, `Conversation`,
  `Message`, `ConversationReadPosition`, `ContentReport`. Public concert browse is served by the read stance.
- **Single-owner filtered:** `Venue`, `Artist` — owner-private reads, with public browse split off to the
  read stance.

## The resource access grant families

One per resource, each over its own scope vocabulary, all deriving from `ResourceAccessGrant<TScope>`:
`ApplicationAccessGrant` (Summary/Proposal), `BookingAccessGrant` (Summary/Operations),
`ContractAccessGrant` (Terms), `ConcertAccessGrant` (Summary/Operations/Finance), `InvoiceAccessGrant`
(Invoice), `ConversationAccessGrant` (Read/SendMessages). Each is a child collection of its own aggregate, so a
resource and its principals' access commit together.

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
