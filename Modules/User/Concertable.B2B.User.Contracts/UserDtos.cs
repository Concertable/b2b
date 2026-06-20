using System.Text.Json.Serialization;
using Concertable.B2B.Tenant.Contracts;
using Concertable.Kernel.Identity;

namespace Concertable.B2B.User.Contracts;

[JsonDerivedType(typeof(VenueManagerDto), "venueManager")]
[JsonDerivedType(typeof(ArtistManagerDto), "artistManager")]
[JsonDerivedType(typeof(AdminDto), "admin")]
public abstract record UserBase : IUser
{
    public Guid Id { get; init; }
    public required string Email { get; init; }
    public abstract Role Role { get; }
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
    public string? County { get; init; }
    public string? Town { get; init; }
    public abstract string BaseUrl { get; init; }
    public bool IsEmailVerified { get; init; }

    /// <summary>The caller's tenant memberships, populated only by <c>GET /api/auth/me</c> to feed the tenant
    /// switcher — empty for cross-module reads. Transitional alongside the <c>Role</c>/<c>BaseUrl</c> shape;
    /// Phase 7 collapses this whole polymorphic DTO into one membership-shaped <c>Me</c>.</summary>
    public IReadOnlyList<MembershipDto> Memberships { get; init; } = [];
}

public sealed record AdminDto : UserBase
{
    public override Role Role { get; } = Role.Admin;
    public override string BaseUrl { get; init; } = "/admin";
}

public sealed record VenueManagerDto : UserBase
{
    public override Role Role { get; } = Role.VenueManager;
    public int? VenueId { get; init; }
    public override string BaseUrl { get; init; } = "/venue";
}

public sealed record ArtistManagerDto : UserBase
{
    public override Role Role { get; } = Role.ArtistManager;
    public int? ArtistId { get; init; }
    public override string BaseUrl { get; init; } = "/artist";
}
