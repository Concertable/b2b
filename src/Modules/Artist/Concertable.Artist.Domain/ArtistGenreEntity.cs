using Concertable.Shared;

namespace Concertable.Artist.Domain;

public class ArtistGenreEntity
{
    public int ArtistId { get; set; }
    public Genre Genre { get; set; }
    public ArtistEntity Artist { get; set; } = null!;
}
