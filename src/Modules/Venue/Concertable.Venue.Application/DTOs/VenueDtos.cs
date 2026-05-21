namespace Concertable.Venue.Application.DTOs;

public record VenueDto : IAddress
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string About { get; set; }
    public double Rating { get; set; }
    public required double Latitude { get; set; }
    public required double Longitude { get; set; }
    public required string BannerUrl { get; set; }
    public string? Avatar { get; set; }
    public required string County { get; set; }
    public required string Town { get; set; }
    public bool Approved { get; set; } = false;
    public required string Email { get; set; }
}
