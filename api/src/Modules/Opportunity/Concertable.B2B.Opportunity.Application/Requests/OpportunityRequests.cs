using Concertable.B2B.Opportunity.Application.Interfaces;
using Concertable.DataAccess.Application.Diffing;
using Concertable.Contracts;

namespace Concertable.B2B.Opportunity.Application.Requests;

internal sealed record OpportunityRequest : ISyncRequest
{
    public int? Id { get; init; }
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public IReadOnlyList<Genre> Genres { get; init; } = [];
    public required DealDto Deal { get; init; }
}
