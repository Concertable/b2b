using Concertable.Contracts;

namespace Concertable.B2B.Seed.Contracts.Specs;

public sealed record ArtistSeedSpec
{
    public required int ArtistId { get; init; }
    public required Guid UserId { get; init; }
    public required string Name { get; init; }
    public required string About { get; init; }
    public required string Avatar { get; init; }
    public required string BannerUrl { get; init; }
    public required string County { get; init; }
    public required string Town { get; init; }
    public required double Latitude { get; init; }
    public required double Longitude { get; init; }
    public required string Email { get; init; }
    public required IReadOnlyCollection<Genre> Genres { get; init; }
}
