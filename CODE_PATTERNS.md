# B2B — structural rosters

B2B's own precedents for two patterns whose generic shape lives in the `multitenancy` and
`keyed-strategies` skills. Read those first; this file is only the roster of real types, which they
deliberately omit. Nothing here restates a rule.

## The DbContext stances, per module

The bases live in `B2B.DataAccess.Infrastructure`; each concrete context lives in its own module's
`Infrastructure/Data/`. Each composes the module's anemic `XConfigurationProvider`; none modifies it.

| Stance | Base | Concrete examples |
|---|---|---|
| Tenant-filtered (both venue↔artist pair and single owner) | `TenantScopedDbContext` | `ConcertDbContext`, `BookingDbContext` (pair); `VenueDbContext` (filters `Venue`/`VenueImage`), `ArtistDbContext` (single owner) |
| Tenant-independent read, `SaveChanges` throws | `ReadDbContext` (shared DataAccess) | `Application`, `Artist`, `Booking`, `Concert`, `Opportunity`, `Venue` |
| Unscoped but writable | `PrivilegedDbContext` | `ConversationsPrivilegedDbContext` (moderation) |
| Untenanted module | `DbContextBase` + own `OnModelCreating` | `Admin`, `Deal`, `Tenant`, `User` — no base owns their `OnModelCreating`; `api/TECH_DEBT.md` holds the repo-wide entry |

One base covers both tenant-filtered stances: the pair/single-owner distinction is carried entirely by which
helper the context's `ApplyTenantFilters` calls, so a separate `VenueArtistTenantScopedDbContext` base bought
nothing and no longer exists. The **repository** pair is a real distinction and does survive —
`VenueArtistTenantScopedRepository` adds `GetTenantPairAsync` / `GetVenueTenantIdAsync` /
`GetArtistTenantIdAsync`, which need both columns.

Filters are declared per entity through the abstract `ApplyTenantFilters` hook —
`modelBuilder.ApplyVenueArtist<TEntity>(this)` or `modelBuilder.ApplySingleOwner<TEntity>(this)` — never
auto-derived from the `IVenueArtistTenantScoped` / `ITenantScoped` marker.

Query classes split by stance: `XRepository` (tenant-bound), `XReadRepository` (`XReadDbContext`),
`XPrivilegedRepository` (writable `PrivilegedDbContext`, only where a cross-tenant write flow exists, e.g.
`MessagePrivilegedRepository`, `ContentReportPrivilegedRepository`). A service holding both `repository` and `readRepository` is the convention when it
injects both stances of its own aggregate. A domain fact that is not naturally an entity repository may get
its own purpose-named abstraction over the read context — `IConcertAvailability`.

## Which entities are filtered

- **Unfiltered by design:** `Opportunity` (the applying artist reads the venue's opportunity to stamp the
  deal), `Deal` (the applying artist reads the venue's terms), `Concert` (public listing).
- **Filtered:** `Venue`, `Artist` — owner-private reads, with public browse split off to the read stance.

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
work sits behind operation-named interfaces resolved through `IDealStrategyFactory<TStrategy>`:
`IApply` (Application), `IConfirm`/`ICancel`/`IContractFactory` (Booking), `ICancel`/`IComplete` (Concert).

## The `DealType` unions

Where the variation is data rather than injected behaviour, `DealType` selects a type, not a strategy.

| Union | Arms | Role |
|---|---|---|
| `DealEntity` | `FlatFeeDealEntity`, `DoorSplitDealEntity`, `VersusDealEntity`, `VenueHireDealEntity` | the editable offer; TPH, each leaf overriding `DealType` |
| `ConfirmedBookingTerms` | `FlatFee`, `VenueHire`, `DoorSplit`, `Versus` | the frozen economics carried on `ConfirmedBookingSnapshot` across the Booking→Concert seam |

`AcceptedApplication` is deliberately *not* a union: once Payment owned the payment-method commitment the
Accept arms became identical, so it is one record carrying the immutable `ApplicationAcceptanceSnapshot`.

`BookingEntity` is the exception, not the pattern: two arms (`Standard`, `Deferred`) over four deal types,
so each leaf re-asks `DealType` — `src/Modules/Booking/TECH_DEBT.md` holds the shape that resolves it.

## Capability, not `DealType`

The concerns partition the four types differently, so no one hierarchy serves them all:

| Concern | Types |
|---|---|
| Door revenue drives settlement | DoorSplit, Versus |
| `FinancialOperation` raised at confirmation | FlatFee (capture), VenueHire (deposit), DoorSplit + Versus (verify) |
| Payment commitment minted at checkout | FlatFee (authorization hold), VenueHire (method setup), DoorSplit + Versus (method verification) |
| Supply direction reverses ([`LEGAL_REQUIREMENTS.md`](./src/Modules/Deal/LEGAL_REQUIREMENTS.md)) | VenueHire |

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
