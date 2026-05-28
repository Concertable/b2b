using Concertable.Kernel;

namespace Concertable.B2B.Concert.Domain.ReadModels;

/// <summary>
/// Denormalized read model of an artist, owned by the Concert module.
/// Kept in sync via <c>ArtistChangedEvent</c> projections so the Concert module
/// can join artist data without crossing into the Artist module's DB context.
/// </summary>
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
