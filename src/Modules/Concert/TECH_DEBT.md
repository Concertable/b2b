# Concert tech debt

## Durable financial lifecycle operations

Accepting, withdrawing, or cancelling an application carries the HTTP request cancellation token through Payment's irreversible capture, deposit, or refund and into the later B2B lifecycle save. A disconnect or process failure after Payment succeeds can therefore leave money moved while the application remains in its previous state. A request-independent token does not close the process-failure window, and the existing Payment event cannot reconstruct the missing B2B transition.

Owner decision: authorize a separate cross-service B2B + Payment saga/package cut-over, or explicitly accept the unresolved financial/state inconsistency risk. The durable design must persist the lifecycle intent and intermediate state before the remote financial operation, stage a transactional outbox command, make Payment operations idempotent by booking, and reconcile pending work in a worker. It must cover cancellation after Payment succeeds.

Resolves when: the cross-service saga is implemented and verified with cancellation-after-payment and process-recovery tests, and application financial state can no longer diverge from Payment after request cancellation or service failure.

## `Genres` allows duplicate tags

`ConcertEntity.Genres` and `OpportunityEntity.Genres` are `List<Genre>`, so the same genre can be added twice — a set is the correct shape for a tag collection. Both are mapped via EF Core's `PrimitiveCollection` (a JSON column), which has a known query-time bug with `ICollection<T>` (dotnet/efcore#35502) and no confirmed support for `HashSet<T>`; switching the backing type needs that verified against this EF Core version before landing, not assumed.

Owner decision: verify `PrimitiveCollection` + `HashSet<Genre>` querying (`.Contains`, filtering) works correctly on the pinned EF Core version, then change both properties and re-scaffold the Concert migration.

Resolves when: `Genres` is a set-shaped type on both entities and a test proves a duplicate genre cannot be added.

## `ContractIssuer.IssueAsync` throws for a missing application instead of returning a Result

`IssueAsync` returns plain `Task`, so both of its lookups end in `OrNotFound` and a missing application or opportunity leaves the method as a `NotFoundException`. Neither is an exceptional condition: contract issuing runs inside the acceptance flow, where the caller already decides between lifecycle outcomes, and `result-carriers` puts an outcome the caller must decide about in a `Result<T, TError>` rather than an exception. The exception also crosses the Infrastructure boundary untyped, so a caller cannot distinguish a missing application from a missing opportunity without catching and inspecting a message.

Owner decision: change `IssueAsync` to return `UnitResult<TError>` over a Concert error union covering both lookups, and adapt the acceptance path that calls it. The two `OrNotFound` calls become `OrFailure`, and the error surfaces through the existing lifecycle error terminal rather than an exception filter.

Resolves when: `IssueAsync` returns a Result, neither lookup throws, and a test proves a missing application yields the typed error rather than a `NotFoundException`.
