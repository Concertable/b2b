using System.Linq.Expressions;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.Kernel.Specifications;

namespace Concertable.B2B.Concert.Infrastructure.Specifications;

internal interface IDoorRevenueOutstandingSpecification : IPredicateSpecification<ConcertEntity> { }

/// <summary>
/// A revenue-share gig whose venue has not yet declared the door take — not ready to settle. The
/// completion sweep skips it (negated); the venue dashboard counts it. One definition, both consumers.
/// </summary>
internal sealed class DoorRevenueOutstandingSpecification
    : PredicateSpecification<ConcertEntity>, IDoorRevenueOutstandingSpecification
{
    protected override Expression<Func<ConcertEntity, bool>> Predicate =>
        c => c.Booking is DeferredBooking && c.DoorRevenue == null;
}
