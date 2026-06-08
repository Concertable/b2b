using Concertable.Kernel;

namespace Concertable.B2B.Venue.Application.DTOs;

public sealed record VenueDto : IAddress
{
    public int Id { get; init; }
    public required string Name { get; init; }
    public required string About { get; init; }
    public required double Latitude { get; init; }
    public required double Longitude { get; init; }
    public required string BannerUrl { get; init; }
    public string? Avatar { get; init; }
    public required string County { get; init; }
    public required string Town { get; init; }
    public bool Approved { get; init; }
    public required string Email { get; init; }
}

public sealed record VenueDetails : IAddress
{
    public int Id { get; init; }
    public required string Name { get; init; }
    public required string About { get; init; }
    public double Rating { get; init; }
    public required double Latitude { get; init; }
    public required double Longitude { get; init; }
    public required string BannerUrl { get; init; }
    public string? Avatar { get; init; }
    public required string County { get; init; }
    public required string Town { get; init; }
    public bool Approved { get; init; }
    public required string Email { get; init; }
}
