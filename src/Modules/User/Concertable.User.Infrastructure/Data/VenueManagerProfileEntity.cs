namespace Concertable.User.Infrastructure.Data;

internal class VenueManagerProfileEntity
{
    private VenueManagerProfileEntity() { }

    public VenueManagerProfileEntity(Guid sub)
    {
        Sub = sub;
    }

    public Guid Sub { get; private set; }
    public int? VenueId { get; private set; }

    public void AssignVenue(int venueId) => VenueId = venueId;
}
