using Concertable.Shared;

namespace Concertable.Concert.Domain;

public class ArtistReadModelGenre
{
    public int ArtistReadModelId { get; set; }
    public Genre Genre { get; set; }
    public ArtistReadModel Artist { get; set; } = null!;
}
