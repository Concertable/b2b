using System.Linq.Expressions;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.Kernel.Specifications;

namespace Concertable.B2B.Concert.Infrastructure.Specifications;

internal interface IDoorRevenueOutstandingSpecification : IPredicateSpecification<ConcertEntity> { }

internal sealed class DoorRevenueOutstandingSpecification
    : PredicateSpecification<ConcertEntity>, IDoorRevenueOutstandingSpecification
{
    // Cast to the concrete leaves, not to the abstract DoorRevenueConcert: EF cannot translate a downcast
    // to an intermediate TPH type, and this predicate is also composed into a correlated subquery.
    protected override Expression<Func<ConcertEntity, bool>> Predicate =>
        concert => (concert is DoorSplitConcert && ((DoorSplitConcert)concert).DoorRevenue == null)
            || (concert is VersusConcert && ((VersusConcert)concert).DoorRevenue == null);
}
