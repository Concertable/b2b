namespace Concertable.Concert.Domain;

public class ArtistReadModel : IIdEntity
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = null!;
    public string? Avatar { get; set; }
    public string? BannerUrl { get; set; }
    public string? County { get; set; }
    public string? Town { get; set; }
    public string? Email { get; set; }
    public ICollection<ArtistReadModelGenre> Genres { get; set; } = [];
}
