using System.Text.Json.Serialization;
using Concertable.Kernel.Identity;

namespace Concertable.B2B.User.Contracts;

[JsonDerivedType(typeof(VenueManagerDto), "venueManager")]
[JsonDerivedType(typeof(ArtistManagerDto), "artistManager")]
[JsonDerivedType(typeof(AdminDto), "admin")]
public abstract record UserBase : IUser
{
    public Guid Id { get; set; }
    public required string Email { get; set; }
    public abstract Role Role { get; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? County { get; set; }
    public string? Town { get; set; }
    public abstract string BaseUrl { get; set; }
    public bool IsEmailVerified { get; set; }
}

public sealed record AdminDto : UserBase
{
    public override Role Role { get; } = Role.Admin;
    public override string BaseUrl { get; set; } = "/admin";
}

public sealed record VenueManagerDto : UserBase
{
    public override Role Role { get; } = Role.VenueManager;
    public int? VenueId { get; set; }
    public override string BaseUrl { get; set; } = "/venue";
}

public sealed record ArtistManagerDto : UserBase
{
    public override Role Role { get; } = Role.ArtistManager;
    public int? ArtistId { get; set; }
    public override string BaseUrl { get; set; } = "/artist";
}
