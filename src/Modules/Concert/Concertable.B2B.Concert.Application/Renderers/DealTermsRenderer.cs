using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Application.Strategies;

namespace Concertable.B2B.Concert.Application.Renderers;

internal sealed class DealTermsRenderer : IDealTermsRenderer
{
    private readonly IConcertDealStrategyFactory<IDealTerms> terms;

    public DealTermsRenderer(IConcertDealStrategyFactory<IDealTerms> terms)
    {
        this.terms = terms;
    }

    public string Render(DealDto deal) =>
        terms.Create(deal.DealType).Render(deal);
}
