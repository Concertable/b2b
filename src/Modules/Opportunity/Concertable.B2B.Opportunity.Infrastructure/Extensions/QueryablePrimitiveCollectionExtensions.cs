using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Opportunity.Infrastructure.Extensions;

internal static class QueryablePrimitiveCollectionExtensions
{
    extension<TEntity>(IQueryable<TEntity> query)
        where TEntity : class
    {
        public IQueryable<TEntity> WhereEmptyOrOverlaps<TElement>(
            string propertyName,
            IReadOnlyCollection<TElement> values)
        {
            var valueList = values.ToArray();
            return query.Where(entity =>
                EF.Property<List<TElement>>(entity, propertyName).Count == 0 ||
                EF.Property<List<TElement>>(entity, propertyName)
                    .Any(value => valueList.Contains(value)));
        }
    }
}
