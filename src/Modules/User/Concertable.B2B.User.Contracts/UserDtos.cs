using System.Text.Json.Serialization;
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
