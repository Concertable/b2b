using Concertable.B2B.Conversations.Infrastructure.Data;
using Concertable.Contracts;
using Concertable.DataAccess.Infrastructure;

namespace Concertable.B2B.Conversations.Infrastructure.Repositories;

internal sealed class ContentReportAdminRepository(ConversationsAdminDbContext context)
    : Repository<ContentReportEntity, ConversationsAdminDbContext, int>(context), IContentReportAdminRepository
{
    public Task<IPagination<ContentReportEntity>> GetQueueAsync(IPageParams pageParams) =>
        base.context.ContentReports
            .OrderByDescending(r => r.SubmittedAt)
            .ToPaginationAsync(pageParams);
}
