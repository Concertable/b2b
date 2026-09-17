namespace Concertable.B2B.User.Contracts;

/// <summary>The subject's portable B2B User fragment for a GDPR access/portability export (arts. 15/20).</summary>
public sealed record SubjectProfileDto
{
    public required string Email { get; init; }
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
    public string? County { get; init; }
    public string? Town { get; init; }
    public string? Avatar { get; init; }
}
