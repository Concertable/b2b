using Concertable.B2B.Conversations.Infrastructure.Data;
using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.DataAccess.Application;
using Concertable.DataAccess.Infrastructure;
using Concertable.Messaging.Infrastructure.Outbox;

namespace Concertable.B2B.Conversations.Infrastructure;

internal interface IOutboxUnitOfWorkBehavior : IOutboxUnitOfWorkBehavior<ConversationsDbContext>;

internal sealed class OutboxUnitOfWorkBehavior(
    ConversationsDbContext context,
    CommandTransactionFactory transactions,
    CommandTransactionAccessor commandAccessor,
    IDbContextAccessor outboxAccessor)
    : CommandOutboxUnitOfWorkBehavior<ConversationsDbContext>(
        context, transactions, commandAccessor, outboxAccessor),
        IOutboxUnitOfWorkBehavior;
