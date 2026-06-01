using Concertable.Contracts;

namespace Concertable.B2B.Artist.Api.Responses;

public sealed record ArtistDetailsResponse
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string About { get; set; }
    public required string BannerUrl { get; set; }
    public string? Avatar { get; set; }
    public double Rating { get; set; }
    public IReadOnlyList<Genre> Genres { get; set; } = [];
    public required string County { get; set; }
    public required string Town { get; set; }
    public required string Email { get; set; }
}
