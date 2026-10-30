# EF Core persistence

[Pattern index](../CODE_PATTERNS.md). Generic repository bindings and migration ownership live in
`persistence`; capability and query naming live in `repository-naming`.

B2B context/repository stances are in [Tenant and resource access](RESOURCE_ACCESS.md).
Commit coordination is in [EF Core transactions and unit of work](EF_CORE_TRANSACTIONS.md).

## Owned child collections without their own repository

The `persistence` skill's "one repository per entity" rule has one sanctioned exception: an entity that is
always read or written jointly with one owning aggregate, and never queried independently, remains mapped by
the owning aggregate's context and is accessed through that aggregate without an independent repository.
`ConcertImageEntity` is mapped by `ConcertDbContext` and reached through `ConcertEntity.Images`. `ConversationReadPosition` has its own `ConversationReadPositionRepository`, which owns read-position
advancement. The moment a consumer needs an owned child independently of its owner, that need earns it a
real repository.
