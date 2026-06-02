using Concertable.Contracts;

namespace Concertable.B2B.Artist.Api.Responses;

public sealed record ArtistDetailsResponse
{
    public int Id { get; init; }
    public required string Name { get; init; }
    public required string About { get; init; }
    public required string BannerUrl { get; init; }
    public string? Avatar { get; init; }
    public double Rating { get; init; }
    public IReadOnlyList<Genre> Genres { get; init; } = [];
    public required string County { get; init; }
    public required string Town { get; init; }
    public required string Email { get; init; }
}
