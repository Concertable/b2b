# Tenant and resource access

[Pattern index](../CODE_PATTERNS.md). Generic policy ownership: `multitenancy`.

## The DbContext stances, per module

The bases live in `B2B.DataAccess.Infrastructure`; each concrete context lives in its own module's
`Infrastructure/Data/`. Each composes the module's anemic `XConfigurationProvider`; none modifies it.

| Stance | Base | Concrete examples |
|---|---|---|
| Tenant-filtered single owner | `TenantScopedDbContext` | `VenueDbContext`, `ArtistDbContext`, `OpportunityDbContext` |
| Principal, assignment and share access | `ResourceScopedDbContext` | `ApplicationDbContext`, `BookingDbContext`, `ConcertDbContext`, `ConversationsDbContext` |
| Tenant-independent read, `SaveChanges` throws | `ReadDbContext` (shared DataAccess) | `Application`, `Artist`, `Booking`, `Concert`, `Opportunity`, `Venue` |
| Unscoped but writable | `PrivilegedDbContext` | `ArtistPrivilegedDbContext`, `OpportunityPrivilegedDbContext`, `ConversationsPrivilegedDbContext` |
| Module-owned configuration | `DbContextBase` + own `OnModelCreating` | `Admin`, `Deal`, `Tenant`, `User` |

Each scoped context declares its filters in `ApplyTenantFilters`. Artist, Venue and Opportunity use
`ApplySingleOwner` for their tenant-owned entities. Application, Booking, Concert and Conversations use
principal rules, member assignments and shares for shared visibility; their ordinary contexts inherit `ResourceScopedDbContext`.

Repository examples distinguish visibility from exposed operations: `ArtistRepository` is tenant-bound,
`ArtistReadRepository` uses `ArtistReadDbContext`, and `ArtistPrivilegedReadRepository` exposes
transaction-enlisted queries through `ArtistPrivilegedDbContext`. The latter's read-only contract keeps
`Read` even though its implementation context is writable. `ConcertPrivilegedRepository` exposes both
reads and writes through its privileged context. Generic capability naming belongs to `repository-naming`; EF bindings belong to
`persistence`, and tenant visibility belongs to `multitenancy`.

A service holding both `repository` and `readRepository` uses those fields for the two stances of its own
aggregate. Application owns `IConcertAvailabilityChecker`; its `ConcertAvailabilityChecker` queries
`IApplicationReadDbContext` for opportunity, artist and venue availability.

## Which entities are filtered

- **Owner-filtered:** `Venue`, `Artist`, `Opportunity` in their ordinary tenant contexts; marketplace
  browsing uses the separate read stance.
- **Resource-filtered:** Application, Booking/Contract, Concert/Invoice and Conversations use their
  aggregates' principal, assignment and share rules at the access the permission holds. Public Concert listing uses its read stance.
- **Module-owned, unfiltered context:** Deal; transaction-enlisted cross-module reads use
  `DealPrivilegedReadRepository`.

## Permission-specific reads

The ordinary resource-scoped context answers "may this member see the resource at all" with the
aggregate's principal, assignment and share rules for the request's actor. A read gated by a different
permission ignores the tenant filter and narrows with plain LINQ in the owning repository; writes ask
the aggregate (see [ARCHITECTURE.md § Authorization](../ARCHITECTURE.md#authorization)).

```csharp
private IQueryable<ApplicationEntity> TermsReadable() =>
    context.Actor is { } actor
    && actor.Permissions[TenantPermission.TermsRead] == ResourceAccess.All
        ? context.Applications.IgnoreQueryFilters([TenantFilters.Key])
            .Where(application =>
                application.VenueTenantId == actor.TenantId || application.ArtistTenantId == actor.TenantId)
        : context.Applications.Where(_ => false);
```

Another module never filters a resource it does not own.
