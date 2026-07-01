using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.Lifecycle;

namespace Concertable.B2B.Concert.Infrastructure.Extensions;

internal static class QueryableOpportunityExtensions
{
    public static IQueryable<OpportunityEntity> WhereActive(this IQueryable<OpportunityEntity> query, DateTimeOffset now) =>
        query
            .Where(o => o.Period.Start >= now)
            .WhereOpen();

    public static IQueryable<OpportunityEntity> WhereOpen(this IQueryable<OpportunityEntity> query) =>
        query.Where(o => !o.Applications.Any(a =>
            a.State != LifecycleState.Applied &&
            a.State != LifecycleState.Rejected &&
            a.State != LifecycleState.Withdrawn));
}
