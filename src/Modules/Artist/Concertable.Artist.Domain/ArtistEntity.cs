using Concertable.Artist.Domain.Events;
using Concertable.Shared;
using NetTopologySuite.Geometries;

namespace Concertable.Artist.Domain;

public class ArtistEntity : IIdEntity, IHasName, IEventRaiser
{
    private readonly EventRaiser _events = new();

    private ArtistEntity() { }

    public int Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Name { get; private set; } = null!;
    public string About { get; private set; } = null!;
    public string BannerUrl { get; private set; } = null!;
    public Point Location { get; private set; } = null!;
    public Address Address { get; private set; } = null!;
    public string Avatar { get; private set; } = null!;
    public string Email { get; private set; } = null!;

    public List<Genre> Genres { get; private set; } = [];

    public IReadOnlyList<IDomainEvent> DomainEvents => _events.DomainEvents;
    public void ClearDomainEvents() => _events.Clear();

    public static ArtistEntity Create(
        Guid userId,
        string name,
        string about,
        string bannerUrl,
        string avatar,
        Point location,
        Address address,
        string email,
        IEnumerable<Genre> genres)
    {
        Validate(name, about, bannerUrl, avatar, location, address, email);

        var artist = new ArtistEntity
        {
            UserId = userId,
            Name = name,
            About = about,
            BannerUrl = bannerUrl,
            Avatar = avatar,
            Location = location,
            Address = address,
            Email = email
        };

        artist.SyncGenresInternal(genres);
        artist._events.Raise(new ArtistChangedDomainEvent(artist));

        return artist;
    }

    public void Update(string name, string about, string bannerUrl, IEnumerable<Genre> genres)
    {
        Validate(name, about, bannerUrl, Avatar, Location, Address, Email);

        Name = name;
        About = about;
        BannerUrl = bannerUrl;

        SyncGenresInternal(genres);
        _events.Raise(new ArtistChangedDomainEvent(this));
    }

    public void SyncGenres(IEnumerable<Genre> genres)
    {
        SyncGenresInternal(genres);
        _events.Raise(new ArtistChangedDomainEvent(this));
    }

    public void UpdateAvatar(string avatar)
    {
        if (string.IsNullOrWhiteSpace(avatar)) throw new DomainException("Avatar is required.");
        Avatar = avatar;
        _events.Raise(new ArtistChangedDomainEvent(this));
    }

    public void UpdateLocation(Point location, Address address)
    {
        if (location is null) throw new DomainException("Location is required.");
        if (address is null || string.IsNullOrWhiteSpace(address.County) || string.IsNullOrWhiteSpace(address.Town))
            throw new DomainException("County and Town are required.");
        Location = location;
        Address = address;
        _events.Raise(new ArtistChangedDomainEvent(this));
    }

    public void UpdateEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) throw new DomainException("Email is required.");
        Email = email;
        _events.Raise(new ArtistChangedDomainEvent(this));
    }

    private void SyncGenresInternal(IEnumerable<Genre> genres) =>
        Genres = genres.ToList();

    private static void Validate(string name, string about, string bannerUrl, string avatar, Point location, Address address, string email)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Name is required.");
        if (string.IsNullOrWhiteSpace(about)) throw new DomainException("About is required.");
        if (string.IsNullOrWhiteSpace(bannerUrl)) throw new DomainException("Banner URL is required.");
        if (string.IsNullOrWhiteSpace(avatar)) throw new DomainException("Avatar is required.");
        if (location is null) throw new DomainException("Location is required.");
        if (address is null || string.IsNullOrWhiteSpace(address.County) || string.IsNullOrWhiteSpace(address.Town))
            throw new DomainException("County and Town are required.");
        if (string.IsNullOrWhiteSpace(email)) throw new DomainException("Email is required.");
    }
}
