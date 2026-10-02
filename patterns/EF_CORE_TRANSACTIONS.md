# EF Core transactions and unit of work

[Pattern index](../CODE_PATTERNS.md) · [Persistence](EF_CORE_PERSISTENCE.md) ·
[Tenant and resource access](RESOURCE_ACCESS.md)

This document owns B2B's transaction precedent and its safety conditions. Framework semantics link to
their official owners. Generic .NET conventions belong in the authored `tj-agents/dotnet` source;
its transaction-pattern extraction is pending with that checkout's existing owner. The installed
`persistence` skill's ambient-scope carrier example does not describe B2B's current implementation.

## Select the boundary from the required work

A `DbContext` tracks one unit of work. Keep it short-lived, dispose it through its owner, and await each
operation before using it again. Concurrent reads use separate contexts and connections.
[EF context lifetime](https://learn.microsoft.com/en-us/ef/core/dbcontext-configuration/#the-dbcontext-lifetime).

With a transactional provider and normal automatic-transaction settings, one `SaveChangesAsync` applies
its changes atomically. An explicit transaction is useful for several saves, raw SQL plus EF writes,
or reads and locks that must participate in the same protected operation. Multiple relational contexts
must share both the connection and transaction. Savepoints can recover a failed save within an existing
transaction where supported; they do not determine the business operation's success. Ambient
`TransactionScope` support is provider-dependent, requires async flow for awaited work, and has synchronous
completion; it is not B2B's default.
[EF transactions](https://learn.microsoft.com/en-us/ef/core/saving/transactions).

| B2B operation | Boundary |
|---|---|
| Ordinary read or parallel dashboard reads | Independently configured context/connection; no coordinator |
| Isolated write needing only one context/save and no protected-command pipeline | Normal EF save, through the selected module abstraction |
| Protected mutation, even with one context | Current B2B unit of work, because locks, outbox work and final authority checks outlive an individual save |
| Several module contexts in one database operation | One B2B unit of work, sequential enlistment and flushing |
| Calls to a payment gateway or another service | Local state/outbox commit followed by the external operation; the database transaction does not make external effects atomic |

## Name the actual responsibility

| Type or API | Responsibility and owner |
|---|---|
| `DbConnection`, `DbTransaction` | Standard relational boundary; `DbTransaction` also implements synchronous `IDbTransaction` and supports async methods. Use `IDbTransaction` where a synchronous consumer actually requires it. [ADO.NET transaction API](https://learn.microsoft.com/en-us/dotnet/api/system.data.common.dbtransaction?view=net-10.0) |
| `IDbContextTransaction` | EF's transaction API, returned by EF rather than implemented as a new application wrapper. [EF transaction API](https://learn.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.storage.idbcontexttransaction?view=efcore-10.0) |
| `NpgsqlDataSource` | Provider creation/configuration and pooling boundary. B2B registers one singleton source. [Npgsql data sources](https://www.npgsql.org/doc/basic-usage.html#data-source) |
| [`UnitOfWork`](../api/src/Concertable.B2B.DataAccess/Concertable.B2B.DataAccess.Infrastructure/UnitOfWork.cs) | Owns the shared connection/transaction, enlisted contexts, repeated flushes and final authority validation |
| [`UnitOfWorkRunner`](../api/src/Concertable.B2B.DataAccess/Concertable.B2B.DataAccess.Infrastructure/UnitOfWorkRunner.cs) | Executes module work; joins a current unit or creates and completes the root |
| [`UnitOfWorkAccessor`](../api/src/Concertable.B2B.DataAccess/Concertable.B2B.DataAccess.Infrastructure/UnitOfWorkAccessor.cs) | Exposes the scoped current coordinator |
| [`ITransactionRunner`](../api/src/Concertable.B2B.DataAccess/Concertable.B2B.DataAccess.Infrastructure/ITransactionRunner.cs) / [`TransactionRunner`](../api/src/Concertable.B2B.DataAccess/Concertable.B2B.DataAccess.Infrastructure/TransactionRunner.cs) | Service-operation port that creates a fresh DI scope and owns root completion |
| [`UnitOfWorkBehavior`](../api/src/Concertable.B2B.DataAccess/Concertable.B2B.DataAccess.Infrastructure/UnitOfWorkBehavior.cs), [`OutboxUnitOfWorkBehavior`](../api/src/Concertable.B2B.DataAccess/Concertable.B2B.DataAccess.Infrastructure/OutboxUnitOfWorkBehavior.cs) | Adapts the selected module carriers to B2B coordination; the outbox variant selects the business context for event records |
| [`IAuthorizationContext`](../api/src/Modules/Authorization/Concertable.B2B.Authorization.Contracts/IAuthorizationContext.cs) | Registers required authority checks/failure outcomes and exposes the coordinator identity as `UnitOfWorkId` |
| [`ITransactionCommitter`](../api/src/Concertable.B2B.DataAccess/Concertable.B2B.DataAccess.Infrastructure/TransactionCommitter.cs) | Narrow commit seam; the provider test injects loss of the commit acknowledgement |

An interface earns its place through the operation exposed to consumers, a module boundary, or a real
substitution such as the commit test seam. The concrete coordinator and accessor do not need matching
interfaces solely for symmetry. Qualify kernel/B2B types with their namespaces where their simple names
coincide. B2B's coordinator adds demonstrated behavior; database transaction creation remains the
provider's operation.

## One owner, several participants

[`AddUnitOfWork`](../api/src/Concertable.B2B.DataAccess/Concertable.B2B.DataAccess.Infrastructure/Extensions/UnitOfWorkExtensions.cs)
registers the provider source, scoped coordinator access and service-operation port. The root opens a
connection and begins a `ReadCommitted` database transaction. `EnlistAsync` accepts only contexts whose
existing connections are closed, attaches the shared connection with `contextOwnsConnection: false`,
and uses `UseTransactionAsync` to attach the transaction. Enlistment is idempotent for the same context.
The framework method takes a `DbTransaction` and returns an EF `IDbContextTransaction` handle.
[UseTransactionAsync](https://learn.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.relationaldatabasefacadeextensions.usetransactionasync?view=efcore-10.0).

The root owns commit, rollback and disposal. Participant contexts borrow the database objects; their EF
handles are released before B2B closes the shared connection and assigns fresh context-owned connections.
`UnitOfWorkRunner` joins nested module work without a second commit. A nested failure result or exception
poisons the whole operation even when an outer delegate returns success. Only the root classifies an
expected database failure after rollback; nested `TryExecuteAsync` lets the failure reach it.

`TransactionRunner` creates a fresh DI scope for a service operation. It is a root entry point, not the
module-participation API. Call nested module behavior through `UnitOfWorkRunner`/the registered behavior;
calling the root service port again would create a separate unit of work.

`FlushAsync` saves participants until no tracked changes remain, selecting each current business context
for outbox insertion. Then the coordinator runs the registered authority checks, including checks added
during flushing. Managed authority must be resolved and no tracked changes may remain before commit.
Permission denial poisons the unit and cannot be swallowed into a successful commit. Resource proofs
bind to this unit's `UnitOfWorkId`; keeping the original locks and authority versions is part of the
operation's consistency requirement. `ReadCommitted` alone is not the authorization fence.

## Failure, cancellation and unknown commit outcomes

B2B marks the commit attempted before invoking the provider. Once attempted, it never treats an exception
as proof of rollback and never replays the delegate. Failure-path participant release and disposal after a commit attempt are best-effort. A release
error after a successful provider commit still propagates; it does not undo the committed operation. Before commit, rollback clears staged tracked changes and releases
participants. An unsuccessful enlistment poisons the unit. Outbox insertion and domain writes remain
in the same database transaction; dispatch happens later.

A connection failure during commit can leave the caller unable to know whether the database committed.
EF execution strategies require explicit transactions to be handled as one retryable operation;
recovery from an unknown commit needs verification/idempotency, rather than an assumption of failure.
[EF connection resiliency](https://learn.microsoft.com/en-us/ef/core/miscellaneous/connection-resiliency#transaction-commit-failure-and-the-idempotency-issue).
B2B currently propagates that outcome without an automatic whole-operation retry.

Await connection open, begin, enlist, flush, validation and completion, passing cancellation as their
contracts require. Caller cancellation does not prove rollback after a commit attempt. The module runner
uses `CancellationToken.None` on its pre-commit failure rollback so an already cancelled caller cannot
suppress that cleanup. The service runner passes `ct` on its exception/explicit validation rollback;
its final `DisposeAsync` also attempts rollback without that token when commit was not attempted.
These are the current paths, not a claim that every cleanup stage is immune to cancellation or provider
errors. Do not enable implicit replay or change these algorithms through a naming/doc change.

## Minimal shared-transaction example

This excerpt uses the DataAccess integration fixture's `CommitProbeDbContext`, `CommitProbeFlushHook`
and `CommitProbe` after `PrepareCommitProbeAsync` creates its test table. It illustrates the standard
database boundary only; protected production writes use the B2B coordinator above.

```csharp
await using DbConnection connection = await dataSource.OpenConnectionAsync(ct);
await using DbTransaction transaction = await connection.BeginTransactionAsync(ct);
var options = new DbContextOptionsBuilder<CommitProbeDbContext>()
    .UseNpgsql(connection, contextOwnsConnection: false)
    .Options;
await using var first = new CommitProbeDbContext(options, new CommitProbeFlushHook());
await using var second = new CommitProbeDbContext(options, new CommitProbeFlushHook());
await first.Database.UseTransactionAsync(transaction, ct);
await second.Database.UseTransactionAsync(transaction, ct);
first.Probes.Add(new CommitProbe(Guid.NewGuid()));
second.Probes.Add(new CommitProbe(Guid.NewGuid()));
await first.SaveChangesAsync(ct);
await second.SaveChangesAsync(ct);
await transaction.CommitAsync(ct);
```

Required namespaces are `System.Data.Common`, `Microsoft.EntityFrameworkCore` and `Npgsql`; `dataSource`
is the fixture's configured `NpgsqlDataSource`, and `ct` is the caller token. Contexts are disposed before
the borrowed transaction/connection by reverse declaration order. The example has one commit owner and
sequential context use. For explicit failure rollback before commit, use a cleanup token independent of
caller cancellation; an exception after commit starts retains the unknown-outcome distinction.

## Verification owners

[`TransactionRunnerTests`](../api/src/Concertable.B2B.DataAccess/Tests/Concertable.B2B.DataAccess.IntegrationTests/TransactionRunnerTests.cs)
uses real PostgreSQL to prove ignored authority denial, validators registered during flush, flush-time
poisoning, preservation of an existing domain failure, ordinary commit, and no replay after a committed
transaction loses its acknowledgement. The module-runner cases exercise the same required-check behavior.
[`ApiFixture`](../api/src/Concertable.B2B.DataAccess/Tests/Concertable.B2B.DataAccess.IntegrationTests/ApiFixture.cs)
owns the concrete commit seam and probe model.

[`ResultOutcomeTests`](../api/src/Concertable.B2B.DataAccess/Tests/Concertable.B2B.DataAccess.UnitTests/ResultOutcomeTests.cs)
checks the selected result-family failure classification.
[`StartupTests`](../api/tests/Concertable.B2B.StartupTests)
checks the Web, Workers and seed registration graphs.
Resource/membership race acceptance belongs to the owning module's provider tests; these eight
transaction tests do not claim full security, nested-failure or outbox-dispatch coverage.
