namespace Concertable.B2B.Application.Infrastructure.Services;

internal sealed class DealTermsSerializer(IStepResolver<IDealTerms> terms) : IDealTermsSerializer
{
    public string Serialize(DealDto deal) => terms.Resolve(deal.DealType).Serialize(deal);
}
