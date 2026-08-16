using Concertable.B2B.Conversations.Infrastructure.Data;
using Concertable.DataAccess.Infrastructure;

namespace Concertable.B2B.Conversations.Infrastructure.Repositories;

internal sealed class MessageAdminRepository(ConversationsAdminDbContext context)
    : Repository<MessageEntity, ConversationsAdminDbContext, int>(context), IMessageAdminRepository;
