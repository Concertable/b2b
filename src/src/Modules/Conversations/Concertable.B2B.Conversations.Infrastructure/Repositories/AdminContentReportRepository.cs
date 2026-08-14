using Concertable.B2B.Conversations.Infrastructure.Data;
using Concertable.DataAccess.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Conversations.Infrastructure.Repositories;

internal sealed class AdminContentReportRepository(AdminConversationsDbContext context)
    : Repository<ContentReportEntity, AdminConversationsDbContext, int>(context), IAdminContentReportRepository
{
    public async Task<IReadOnlyList<ContentReportEntity>> GetQueueAsync() =>
        await base.context.ContentReports
            .OrderByDescending(r => r.SubmittedAt)
            .ToListAsync();
}
