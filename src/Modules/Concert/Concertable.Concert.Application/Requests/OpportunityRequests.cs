using Concertable.DataAccess.Diffing;
using Concertable.Concert.Application.Interfaces;
using Concertable.Shared;

namespace Concertable.Concert.Application.Requests;

internal record OpportunityRequest : ISyncRequest
{
    public int? Id { get; init; }
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public IReadOnlyList<Genre> Genres { get; init; } = [];
    public required IContract Contract { get; init; }
}
