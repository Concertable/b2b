namespace Concertable.User.Infrastructure.Data;

internal class ArtistManagerProfileEntity
{
    private ArtistManagerProfileEntity() { }

    public ArtistManagerProfileEntity(Guid sub)
    {
        Sub = sub;
    }

    public Guid Sub { get; private set; }
    public int? ArtistId { get; private set; }

    public void AssignArtist(int artistId) => ArtistId = artistId;
}
