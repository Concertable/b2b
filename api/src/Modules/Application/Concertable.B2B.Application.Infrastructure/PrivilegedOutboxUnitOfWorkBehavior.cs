using Concertable.B2B.Application.Infrastructure.Data;
using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.DataAccess.Application;
using Concertable.Messaging.Infrastructure.Outbox;

namespace Concertable.B2B.Application.Infrastructure;

internal interface IPrivilegedOutboxUnitOfWorkBehavior
    : IOutboxUnitOfWorkBehavior<ApplicationPrivilegedDbContext>;

internal sealed class PrivilegedOutboxUnitOfWorkBehavior(
    ApplicationPrivilegedDbContext context,
    CommandTransactionFactory transactions,
    CommandTransactionAccessor commandAccessor,
    IDbContextAccessor outboxAccessor)
    : CommandOutboxUnitOfWorkBehavior<ApplicationPrivilegedDbContext>(
        context, transactions, commandAccessor, outboxAccessor),
        IPrivilegedOutboxUnitOfWorkBehavior;
