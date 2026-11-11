# B2B pattern index

Choose the document for the pattern being changed. These documents own B2B implementations,
rosters and exceptions; generic standards retain their own source owners.

| Pattern | B2B document | Generic owner |
|---|---|---|
| Tenant visibility and resource access | [Tenant and resource access](patterns/RESOURCE_ACCESS.md) | `multitenancy` |
| Repository/context bindings and owned children | [EF Core persistence](patterns/EF_CORE_PERSISTENCE.md) | `persistence`, `repository-naming` |
| Database transactions and application coordination | [EF Core transactions and unit of work](patterns/EF_CORE_TRANSACTIONS.md) | [EF/.NET/provider documentation](https://learn.microsoft.com/en-us/ef/core/saving/transactions) |
| Deal strategies, unions and lifecycle capabilities | [Deal variation](patterns/DEAL_VARIATION.md) | `keyed-strategies`, `keyed-unions` |

Authored generic .NET definitions live in [tj-agents/dotnet](https://github.com/tj-agents/dotnet/tree/main/.agents/contract).
Installing a standards package does not select every optional pattern. B2B transaction precedents use
the explicit shared-connection coordinator described above; selected library aliases do not determine
the transaction mechanism.
