using Concertable.Shared;

namespace Concertable.Artist.Contracts.Views;

public sealed class ArtistView : IIdEntity
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = null!;
    public string? Avatar { get; set; }
    public string? BannerUrl { get; set; }
    public string? County { get; set; }
    public string? Town { get; set; }
    public string? Email { get; set; }
    public ICollection<ArtistViewGenre> Genres { get; set; } = [];
}