using System.Linq.Expressions;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.Lifecycle;
using Concertable.Kernel.Specifications;

namespace Concertable.B2B.Concert.Infrastructure.Specifications;

internal interface IEndedAndBookedSpecification : IPredicateSpecification<ConcertEntity> { }

/// <summary>A concert whose gig has ended while its booking is still Booked — the settlement window.</summary>
internal sealed class EndedAndBookedSpecification
    : PredicateSpecification<ConcertEntity>, IEndedAndBookedSpecification
{
    private readonly TimeProvider timeProvider;

    public EndedAndBookedSpecification(TimeProvider timeProvider) => this.timeProvider = timeProvider;

    protected override Expression<Func<ConcertEntity, bool>> Predicate
    {
        get
        {
            var now = timeProvider.GetUtcNow().UtcDateTime;
            return c => c.Period.End < now && c.Booking.Application.State == LifecycleState.Booked;
        }
    }
}
