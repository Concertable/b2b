using Concertable.B2B.Opportunity.Api.Responses;
using Concertable.B2B.Opportunity.Contracts;

namespace Concertable.B2B.Opportunity.Api.Mappers;

internal interface IOpportunityMapper
{
    Task<OpportunityResponse> ToResponseAsync(
        OpportunityDto opportunity,
        CancellationToken ct = default);
    Task<IReadOnlyList<OpportunityResponse>> ToResponsesAsync(
        IReadOnlyCollection<OpportunityDto> opportunities,
        CancellationToken ct = default);
    Task<IPagination<OpportunityResponse>> ToResponsesAsync(
        IPagination<OpportunityDto> opportunities,
        CancellationToken ct = default);
}
