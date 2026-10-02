# Tenant and resource access

[Pattern index](../CODE_PATTERNS.md). Generic policy ownership: `multitenancy`.

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
reads and writes through its privileged context. Generic capability naming belongs to `repository-naming`; EF bindings belong to
`persistence`, and tenant visibility belongs to `multitenancy`.

A service holding both `repository` and `readRepository` uses those fields for the two stances of its own
aggregate. The domain capability `IConcertAvailability` has its own purpose-named abstraction over the
read context.

## Which entities are filtered

- **Owner-filtered:** `Venue`, `Artist`, `Opportunity` in their ordinary tenant contexts; marketplace
  browsing uses the separate read stance.
- **Resource-filtered:** Application, Booking/Contract, Concert/Invoice and Conversations use live grants
  and the audience required by the operation. Public Concert listing uses its read stance.
- **Module-owned, unfiltered context:** Deal; transaction-enlisted cross-module reads use
  `DealPrivilegedReadRepository`.
