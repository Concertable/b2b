using Concertable.B2B.Conversations.Infrastructure.Data;
using Concertable.Contracts;
using Concertable.DataAccess.Infrastructure;

namespace Concertable.B2B.Conversations.Infrastructure.Repositories;

internal sealed class ContentReportAdminRepository : Repository<ContentReportEntity, int>, IContentReportAdminRepository
{
    private readonly ConversationsAdminDbContext context;

    public ContentReportAdminRepository(ConversationsAdminDbContext context) : base(context)
    {
        this.context = context;
    }

    public Task<IPagination<ContentReportEntity>> GetQueueAsync(IPageParams pageParams) =>
        context.ContentReports
            .OrderByDescending(r => r.SubmittedAt)
            .ToPaginationAsync(pageParams);
}
