using Concertable.Contracts;
using Microsoft.AspNetCore.Http;

namespace Concertable.B2B.Artist.Application.Requests;

internal record CreateArtistRequest
{
    public required string Name { get; init; }
    public required string About { get; init; }
    public required double Latitude { get; init; }
    public required double Longitude { get; init; }
    public IReadOnlyList<Genre> Genres { get; init; } = [];
    public required IFormFile Banner { get; init; }
    public required IFormFile Avatar { get; init; }
}

internal record UpdateArtistRequest
{
    public required string Name { get; init; }
    public required string About { get; init; }
    public required double Latitude { get; init; }
    public required double Longitude { get; init; }
    public ImageDto? Banner { get; init; }
    public IFormFile? Avatar { get; init; }
    public IReadOnlyList<Genre> Genres { get; init; } = [];
}
