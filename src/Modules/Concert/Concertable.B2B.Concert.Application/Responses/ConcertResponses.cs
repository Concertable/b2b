namespace Concertable.B2B.Concert.Application.Responses;

internal sealed record ConcertUpdateResponse
{
    public int Id { get; init; }
    public required string Name { get; init; }
    public required string About { get; init; }
    public decimal Price { get; init; }
    public int TotalTickets { get; init; }
    public int AvailableTickets { get; init; }
}
