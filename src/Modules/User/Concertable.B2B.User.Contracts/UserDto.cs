using Concertable.B2B.Tenant.Contracts;

namespace Concertable.B2B.User.Contracts;

public sealed record UserDto
{
    public Guid Id { get; init; }
    public required string Email { get; init; }
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
    public string? County { get; init; }
    public string? Town { get; init; }
    public bool IsEmailVerified { get; init; }
    public IReadOnlyList<MembershipDto> Memberships { get; init; } = [];
}

public sealed record ManagerDto
{
    public Guid Id { get; init; }
    public string? Email { get; init; }
    public string? Avatar { get; init; }
}
