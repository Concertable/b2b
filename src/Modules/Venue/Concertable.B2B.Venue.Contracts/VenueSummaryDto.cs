namespace Concertable.B2B.Venue.Contracts;

public sealed record VenueSummaryDto
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Avatar { get; set; }
    public double Rating { get; set; }
}
