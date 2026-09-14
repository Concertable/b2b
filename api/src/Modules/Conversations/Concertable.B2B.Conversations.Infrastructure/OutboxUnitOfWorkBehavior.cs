using Concertable.B2B.Conversations.Infrastructure.Data;
using Concertable.DataAccess.Application;
using Concertable.DataAccess.Infrastructure;
using Concertable.Messaging.Infrastructure.Outbox;

namespace Concertable.B2B.Conversations.Infrastructure;

internal interface IOutboxUnitOfWorkBehavior : IOutboxUnitOfWorkBehavior<ConversationsDbContext>;

internal sealed class OutboxUnitOfWorkBehavior(ConversationsDbContext context, IDbContextAccessor accessor)
    : OutboxUnitOfWorkBehavior<ConversationsDbContext>(context, accessor), IOutboxUnitOfWorkBehavior;
