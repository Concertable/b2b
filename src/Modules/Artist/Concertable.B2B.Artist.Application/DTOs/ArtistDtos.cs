using Concertable.Contracts;
using Concertable.Kernel;

namespace Concertable.B2B.Artist.Application.DTOs;

public sealed record ArtistDto : IAddress
{
    public int Id { get; init; }
    public required string Name { get; init; }
    public required string About { get; init; }
    public IEnumerable<Genre> Genres { get; init; } = [];
    public required string BannerUrl { get; init; }
    public string? Avatar { get; init; }
    public required string County { get; init; }
    public required string Town { get; init; }
    public required string Email { get; init; }
}

public sealed record ArtistDetails : IAddress
{
    public int Id { get; init; }
    public required string Name { get; init; }
    public required string About { get; init; }
    public double Rating { get; init; }
    public IEnumerable<Genre> Genres { get; init; } = [];
    public required string BannerUrl { get; init; }
    public string? Avatar { get; init; }
    public required string County { get; init; }
    public required string Town { get; init; }
    public required string Email { get; init; }
}
