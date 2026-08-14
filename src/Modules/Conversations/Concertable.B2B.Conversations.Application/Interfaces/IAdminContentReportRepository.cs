using Concertable.DataAccess.Application;

namespace Concertable.B2B.Conversations.Application.Interfaces;

internal interface IAdminContentReportRepository : IRepository<ContentReportEntity>
{
    /// <summary>The triage queue: every report across every tenant, newest first.</summary>
    Task<IReadOnlyList<ContentReportEntity>> GetQueueAsync();
}
