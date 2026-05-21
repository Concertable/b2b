using Concertable.Shared;

namespace Concertable.Venue.Contracts.Views;

public sealed class VenueView : IIdEntity, IHasName
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = null!;
    public string? Avatar { get; set; }
    public string? BannerUrl { get; set; }
    public string? County { get; set; }
    public string? Town { get; set; }
    public string? Email { get; set; }
}
