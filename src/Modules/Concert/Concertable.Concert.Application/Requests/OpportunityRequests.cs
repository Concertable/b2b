using Concertable.Concert.Application.Interfaces;
using Concertable.DataAccess.Application.Diffing;
using Concertable.Contracts;

namespace Concertable.Concert.Application.Requests;

internal record OpportunityRequest : ISyncRequest
{
    public int? Id { get; init; }
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public IReadOnlyList<Genre> Genres { get; init; } = [];
    public required IContract Contract { get; init; }
}
