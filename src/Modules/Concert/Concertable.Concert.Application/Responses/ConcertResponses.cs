using Concertable.Concert.Application.DTOs;

namespace Concertable.Concert.Application.Responses;

internal record ConcertPostResponse
{
    public required ConcertSnapshot ConcertHeader { get; set; }
    public IReadOnlyList<Guid> UserIds { get; set; } = [];
}

internal record ConcertUpdateResponse
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string About { get; set; }
    public decimal Price { get; set; }
    public int TotalTickets { get; set; }
    public int AvailableTickets { get; set; }
}
