namespace Concertable.Concert.Application.Requests;

internal record UpdateConcertRequest
{
    public required string Name { get; init; }
    public required string About { get; init; }
    public decimal Price { get; init; }
    public int TotalTickets { get; init; }
}
