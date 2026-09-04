using System.Linq.Expressions;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.Kernel.Specifications;

namespace Concertable.B2B.Concert.Infrastructure.Specifications;

internal interface IDoorRevenueOutstandingSpecification : IPredicateSpecification<ConcertEntity> { }

internal sealed class DoorRevenueOutstandingSpecification
    : PredicateSpecification<ConcertEntity>, IDoorRevenueOutstandingSpecification
{
    protected override Expression<Func<ConcertEntity, bool>> Predicate =>
        c => c is DoorRevenueConcert && ((DoorRevenueConcert)c).DoorRevenue == null;
}
