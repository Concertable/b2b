using Concertable.Contract.Contracts;
using Concertable.Contracts;

namespace Concertable.Concert.Application.DTOs;

internal record OpportunityDto
{
    public int Id { get; set; }
    public int VenueId { get; set; }
    public int ContractId { get; set; }
    public IContract Contract { get; set; } = null!;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public IReadOnlyList<Genre> Genres { get; set; } = [];
}
