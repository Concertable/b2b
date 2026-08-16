using Concertable.B2B.Conversations.Infrastructure.Data;

namespace Concertable.B2B.Conversations.Infrastructure.Repositories;

internal sealed class ContentReportRepository(ConversationsDbContext context)
    : Repository<ContentReportEntity>(context), IContentReportRepository;
