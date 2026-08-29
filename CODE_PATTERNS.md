# B2B — structural rosters

B2B's own precedents for two patterns whose generic shape lives in the `multitenancy` and
`keyed-strategies` skills. Read those first; this file is only the roster of real types, which they
deliberately omit. Nothing here restates a rule.

## The DbContext stances, per module

The bases live in `B2B.DataAccess.Infrastructure`; each concrete context lives in its own module's
`Infrastructure/Data/`. Each composes the module's anemic `XConfigurationProvider`; none modifies it.

| Stance | Base | Concrete examples |
|---|---|---|
| Tenant-filtered, venue↔artist pair | `VenueArtistTenantScopedDbContext` | `ConcertDbContext` |
| Tenant-filtered, single owner | `TenantScopedDbContext` | `VenueDbContext` (filters `Venue`/`VenueImage`), `ArtistDbContext` |
| Tenant-independent read, `SaveChanges` throws | `ReadDbContext` (shared DataAccess) | `ConcertReadDbContext` |
| Unscoped but writable | `PrivilegedDbContext` | `ConversationsPrivilegedDbContext` (moderation) |

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

## The workflow steps a `DealType` selects

`IConcertWorkflow` implementations in `Modules/Concert/…/Services/Workflow/Workflows/` surface the five
steps — `Apply`, `Accept`, `Book`, `Finish`, `Cancel` — as public get-only properties. They are the
canonical dependency-holder shape (`dependency-injection` skill): concrete constructor parameters, so DI
resolves the registered step, assigned to interface-typed properties.
