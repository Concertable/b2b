# EF Core persistence

[Pattern index](../CODE_PATTERNS.md). Generic repository bindings and migration ownership live in
`persistence`; capability and query naming live in `repository-naming`.

B2B context/repository stances are in [Tenant and resource access](RESOURCE_ACCESS.md).
Commit coordination is in [EF Core transactions and unit of work](EF_CORE_TRANSACTIONS.md).

## Owned child collections without their own repository

The `persistence` skill's "one repository per entity" rule has one sanctioned exception: an entity that is
always read or written jointly with one owning aggregate, and never queried independently, stays a `DbSet` on
the owning repository rather than gaining a repository of its own. `ConcertImageEntity` is the current
example. `ConversationReadPosition` has its own `ConversationReadPositionRepository`, which owns read-position
advancement. The moment a consumer needs an owned child independently of its owner, that need earns it a
real repository.
