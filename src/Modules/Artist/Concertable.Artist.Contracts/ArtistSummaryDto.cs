using Concertable.Shared;

namespace Concertable.Artist.Contracts;

public record ArtistSummaryDto
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Avatar { get; set; }
    public double Rating { get; set; }
    public IEnumerable<Genre> Genres { get; set; } = [];
}
