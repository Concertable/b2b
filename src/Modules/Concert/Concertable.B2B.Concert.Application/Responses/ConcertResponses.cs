namespace Concertable.B2B.Concert.Application.Responses;

internal sealed record ConcertUpdateResponse
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string About { get; set; }
    public decimal Price { get; set; }
    public int TotalTickets { get; set; }
    public int AvailableTickets { get; set; }
}
