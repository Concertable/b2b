using Concertable.Contracts;
using Concertable.DataAccess.Application;

namespace Concertable.B2B.Conversations.Application.Interfaces;

internal interface IContentReportPrivilegedRepository : IRepository<ContentReportEntity>
{
    /// <summary>The triage queue: reports across every tenant, newest first.</summary>
    Task<IPagination<ContentReportEntity>> GetQueueAsync(IPageParams pageParams);
}
