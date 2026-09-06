using System.Linq.Expressions;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.Kernel.Specifications;

namespace Concertable.B2B.Concert.Infrastructure.Specifications;

internal interface IEndedSpecification : IPredicateSpecification<ConcertEntity> { }

internal sealed class EndedSpecification
    : PredicateSpecification<ConcertEntity>, IEndedSpecification
{
    private readonly TimeProvider timeProvider;

    public EndedSpecification(TimeProvider timeProvider) => this.timeProvider = timeProvider;

    protected override Expression<Func<ConcertEntity, bool>> Predicate
    {
        get
        {
            var now = timeProvider.GetUtcNow().UtcDateTime;
            return concert => concert.Period.End < now;
        }
    }
}
