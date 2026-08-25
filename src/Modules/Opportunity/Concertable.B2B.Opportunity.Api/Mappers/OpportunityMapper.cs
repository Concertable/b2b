using Concertable.B2B.Application.Contracts;
using Concertable.B2B.Deal.Contracts;
using Concertable.B2B.Opportunity.Api.Responses;
using Concertable.B2B.Opportunity.Contracts;
using Microsoft.AspNetCore.Http;

namespace Concertable.B2B.Opportunity.Api.Mappers;

internal sealed class OpportunityMapper : IOpportunityMapper
{
    private readonly IDealModule deals;

    public OpportunityMapper(IDealModule deals)
    {
        this.deals = deals;
    }

    public async Task<OpportunityResponse> ToResponseAsync(
        OpportunityDto opportunity,
        CancellationToken ct = default)
    {
        var dealOption = await deals.GetByIdAsync(opportunity.DealId, ct);
        if (!dealOption.TryGetValue(out var deal))
            throw MissingDeal(opportunity);

        return opportunity.ToResponse(deal);
    }

    public async Task<IReadOnlyList<OpportunityResponse>> ToResponsesAsync(
        IReadOnlyCollection<OpportunityDto> opportunities,
        CancellationToken ct = default)
    {
        var dealsById = await GetDealsByIdAsync(opportunities, ct);
        return opportunities
            .Select(opportunity => opportunity.ToResponse(GetDeal(opportunity, dealsById)))
            .ToList();
    }

    public async Task<IPagination<OpportunityResponse>> ToResponsesAsync(
        IPagination<OpportunityDto> opportunities,
        CancellationToken ct = default)
    {
        var dealsById = await GetDealsByIdAsync(opportunities.Data, ct);
        return opportunities.Map(
            opportunity => opportunity.ToResponse(GetDeal(opportunity, dealsById)));
    }

    private async Task<IReadOnlyDictionary<int, DealDto>> GetDealsByIdAsync(
        IReadOnlyCollection<OpportunityDto> opportunities,
        CancellationToken ct) =>
        (await deals.GetByIdsAsync(
            opportunities.Select(opportunity => opportunity.DealId).Distinct(),
            ct)).ToDictionary(deal => deal.Id);

    private static DealDto GetDeal(
        OpportunityDto opportunity,
        IReadOnlyDictionary<int, DealDto> dealsById) =>
        dealsById.TryGetValue(opportunity.DealId, out var deal)
            ? deal
            : throw MissingDeal(opportunity);

    private static InvalidOperationException MissingDeal(OpportunityDto opportunity) =>
        new($"Opportunity {opportunity.Id} references missing deal {opportunity.DealId}.");

}

internal static class OpportunityMappers
{
    extension(OpportunityDto opportunity)
    {
        public OpportunityResponse ToResponse(DealDto deal) =>
            new(
                opportunity.Id,
                opportunity.VenueId,
                deal,
                opportunity.StartDate,
                opportunity.EndDate,
                opportunity.Genres,
                new OpportunityActions(
                    deal.DealType.RequiresApplyCheckout()
                        ? new ActionLink(
                            $"/api/application/opportunity/{opportunity.Id}/checkout",
                            HttpMethods.Post)
                        : null));
    }
}
