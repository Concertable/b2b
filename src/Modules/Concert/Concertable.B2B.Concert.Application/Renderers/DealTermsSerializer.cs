using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Application.Strategies;

namespace Concertable.B2B.Concert.Application.Renderers;

internal sealed class DealTermsSerializer : IDealTermsSerializer
{
    private readonly IConcertDealStrategyFactory<IDealTerms> terms;

    public DealTermsSerializer(IConcertDealStrategyFactory<IDealTerms> terms)
    {
        this.terms = terms;
    }

    public string Serialize(IDeal deal) =>
        terms.Create(deal.DealType).Serialize(deal);
}
