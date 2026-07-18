using System.ComponentModel;
using Concertable.Contracts.Enums;

namespace Concertable.B2B.Artist.Contracts;

[DisplayName(DisplayNames.Artist)]
public sealed record ArtistSummary
{
    public int Id { get; init; }
    public required string Name { get; init; }
    public string? Avatar { get; init; }
    public double Rating { get; init; }
    public IEnumerable<Genre> Genres { get; init; } = [];
}
