using Concertable.B2B.Contract.Contracts;
using Concertable.Contracts;

namespace Concertable.B2B.Concert.Application.DTOs;

internal sealed record OpportunityDto
{
    public int Id { get; init; }
    public int VenueId { get; init; }
    public int ContractId { get; init; }
    public IContract Contract { get; init; } = null!;
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public IReadOnlyList<Genre> Genres { get; init; } = [];
}
