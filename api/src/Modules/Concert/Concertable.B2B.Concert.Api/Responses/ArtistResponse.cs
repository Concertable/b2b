using Concertable.B2B.Concert.Domain.Lifecycle;
using Concertable.Contracts;

namespace Concertable.B2B.Concert.Api.Responses;

internal sealed record ArtistResponse
{
    public int Id { get; init; }
    public required string Name { get; init; }
    public string? Avatar { get; init; }
    public double Rating { get; init; }
    public required string County { get; init; }
    public required string Town { get; init; }
    public IReadOnlyList<Genre> Genres { get; init; } = [];
}
