using Concertable.B2B.Conversations.Infrastructure.Data;
using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.DataAccess.Application;
using Concertable.DataAccess.Infrastructure;
using Concertable.Messaging.Infrastructure.Outbox;

namespace Concertable.B2B.Conversations.Infrastructure;

internal interface IPrivilegedOutboxUnitOfWorkBehavior
    : IOutboxUnitOfWorkBehavior<ConversationsPrivilegedDbContext>;

internal sealed class PrivilegedOutboxUnitOfWorkBehavior(
    ConversationsPrivilegedDbContext context,
    UnitOfWorkRunner unitOfWorkRunner,
    UnitOfWorkAccessor unitOfWorkAccessor,
    IDbContextAccessor outboxAccessor)
    : Concertable.B2B.DataAccess.Infrastructure.OutboxUnitOfWorkBehavior<ConversationsPrivilegedDbContext>(
        context, unitOfWorkRunner, unitOfWorkAccessor, outboxAccessor),
        IPrivilegedOutboxUnitOfWorkBehavior;
