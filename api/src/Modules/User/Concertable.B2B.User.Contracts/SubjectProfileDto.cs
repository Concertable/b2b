namespace Concertable.B2B.User.Contracts;

public sealed record SubjectProfileDto
{
    public required string Email { get; init; }
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
    public string? County { get; init; }
    public string? Town { get; init; }
    public string? Avatar { get; init; }
}
