namespace Concertable.B2B.Venue.Api.Responses;

public sealed record VenueDetailsResponse
{
    public int Id { get; init; }
    public required string Name { get; init; }
    public required string About { get; init; }
    public required string BannerUrl { get; init; }
    public string? Avatar { get; init; }
    public double Rating { get; init; }
    public required string County { get; init; }
    public required string Town { get; init; }
    public required string Email { get; init; }
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public bool Approved { get; init; }
}
