using Concertable.B2B.Application.Application.Strategies;

namespace Concertable.B2B.Application.Infrastructure.Services;

internal sealed class DealTermsSerializer : IDealTermsSerializer
{
    private readonly IApplicationDealStrategyFactory<IDealTerms> terms;

    public DealTermsSerializer(IApplicationDealStrategyFactory<IDealTerms> terms)
    {
        this.terms = terms;
    }

    public string Serialize(DealDto deal) => terms.Create(deal.DealType).Serialize(deal);
}
