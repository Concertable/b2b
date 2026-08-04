using System.ComponentModel;
using Concertable.B2B.Artist.Contracts;
using Concertable.Contracts;
using Concertable.Kernel;

namespace Concertable.B2B.Artist.Application.DTOs;

[DisplayName(DisplayNames.Artist)]
internal sealed record ArtistDetails : IAddress
{
    public int Id { get; init; }
    public required string Name { get; init; }
    public required string About { get; init; }
    public double Rating { get; init; }
    public IEnumerable<Genre> Genres { get; init; } = [];
    public required string BannerUrl { get; init; }
    public required string Avatar { get; init; }
    public required string County { get; init; }
    public required string Town { get; init; }
    public required string Email { get; init; }
}
