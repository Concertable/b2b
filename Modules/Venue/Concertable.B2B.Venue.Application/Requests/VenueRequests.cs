using Concertable.Contracts;
using Microsoft.AspNetCore.Http;

namespace Concertable.B2B.Venue.Application.Requests;

internal sealed record CreateVenueRequest
{
    public required string Name { get; init; }
    public required string About { get; init; }
    public required double Latitude { get; init; }
    public required double Longitude { get; init; }
    public required IFormFile Banner { get; init; }
    public required IFormFile Avatar { get; init; }
}

internal sealed record UpdateVenueRequest
{
    public required string Name { get; init; }
    public required string About { get; init; }
    public bool Approved { get; init; }
    public ImageDto? Banner { get; init; }
    public IFormFile? Avatar { get; init; }
    public required double Latitude { get; init; }
    public required double Longitude { get; init; }
}
