using System.Text.Json.Serialization;

namespace Concertable.User.Contracts;

[JsonDerivedType(typeof(VenueManagerDto), "venueManager")]
[JsonDerivedType(typeof(ArtistManagerDto), "artistManager")]
[JsonDerivedType(typeof(AdminDto), "admin")]
public interface IUser
{
    Guid Id { get; set; }
    string Email { get; set; }
    Role Role { get; }
    double? Latitude { get; set; }
    double? Longitude { get; set; }
    string? County { get; set; }
    string? Town { get; set; }
    string BaseUrl { get; set; }
    bool IsEmailVerified { get; set; }
}

public record AdminDto : IUser
{
    public Guid Id { get; set; }
    public required string Email { get; set; }
    public Role Role { get; } = Role.Admin;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? County { get; set; }
    public string? Town { get; set; }
    public string BaseUrl { get; set; } = "/admin";
    public bool IsEmailVerified { get; set; }
}

public record VenueManagerDto : IUser
{
    public Guid Id { get; set; }
    public required string Email { get; set; }
    public Role Role { get; } = Role.VenueManager;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? County { get; set; }
    public string? Town { get; set; }
    public int? VenueId { get; set; }
    public string BaseUrl { get; set; } = "/venue";
    public bool IsEmailVerified { get; set; }
}

public record ArtistManagerDto : IUser
{
    public Guid Id { get; set; }
    public required string Email { get; set; }
    public Role Role { get; } = Role.ArtistManager;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? County { get; set; }
    public string? Town { get; set; }
    public int? ArtistId { get; set; }
    public string BaseUrl { get; set; } = "/artist";
    public bool IsEmailVerified { get; set; }
}

