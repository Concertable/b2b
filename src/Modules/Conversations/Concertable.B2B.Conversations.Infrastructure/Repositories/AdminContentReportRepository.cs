using Concertable.B2B.Conversations.Infrastructure.Data;
using Concertable.Contracts;
using Concertable.DataAccess.Infrastructure;

namespace Concertable.B2B.Conversations.Infrastructure.Repositories;

internal sealed class AdminContentReportRepository(AdminConversationsDbContext context)
    : Repository<ContentReportEntity, AdminConversationsDbContext, int>(context), IAdminContentReportRepository
{
    public Task<IPagination<ContentReportEntity>> GetQueueAsync(IPageParams pageParams) =>
        base.context.ContentReports
            .OrderByDescending(r => r.SubmittedAt)
            .ToPaginationAsync(pageParams);
}
