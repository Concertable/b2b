using Concertable.Contracts;

namespace Concertable.B2B.Artist.Contracts;

public sealed record ArtistSummaryDto
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Avatar { get; set; }
    public double Rating { get; set; }
    public IEnumerable<Genre> Genres { get; set; } = [];
}
