namespace Concertable.B2B.Concert.Application.Requests;

internal sealed record UpdateConcertRequest
{
    public required string Name { get; init; }
    public required string About { get; init; }
    public decimal Price { get; init; }
    public int TotalTickets { get; init; }
}

internal sealed record DoorRevenueRequest
{
    /// <summary>External take only (excludes Concertable's own ticket sales, which settlement already knows).</summary>
    public decimal DoorRevenue { get; init; }
}
