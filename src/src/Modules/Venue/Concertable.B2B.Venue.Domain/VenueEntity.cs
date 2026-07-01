using Concertable.Kernel;
using Concertable.B2B.Venue.Domain.Events;
using NetTopologySuite.Geometries;

namespace Concertable.B2B.Venue.Domain;

public sealed class VenueEntity : IIdEntity, IHasName, IEventRaiser, ITenantScoped
{
    private readonly EventRaiser events = new();

    private VenueEntity() { }

    public int Id { get; private set; }
    public Guid TenantId { get; set; }
    public Guid UserId { get; private set; }
    public string Name { get; private set; } = null!;
    public string About { get; private set; } = null!;
    public string BannerUrl { get; private set; } = null!;
    public bool Approved { get; private set; }
    public Point Location { get; private set; } = null!;
    public Address Address { get; private set; } = null!;
    public string Avatar { get; private set; } = null!;
    public string Email { get; private set; } = null!;

    public IReadOnlyList<IDomainEvent> DomainEvents => events.DomainEvents;
    public void ClearDomainEvents() => events.Clear();

    public static VenueEntity Create(
        Guid userId,
        string name,
        string about,
        string bannerUrl,
        string avatar,
        Point location,
        Address address,
        string email)
    {
        Validate(name, about, bannerUrl, avatar, location, address, email);

        var venue = new VenueEntity
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
        venue.events.Raise(new VenueChangedDomainEvent(venue));
        return venue;
    }

    public void Update(string name, string about, string bannerUrl)
    {
        Validate(name, about, bannerUrl, Avatar, Location, Address, Email);
        Name = name;
        About = about;
        BannerUrl = bannerUrl;
        events.Raise(new VenueChangedDomainEvent(this));
    }

    public void Approve()
    {
        Approved = true;
        events.Raise(new VenueChangedDomainEvent(this));
    }

    public void UpdateAvatar(string avatar)
    {
        DomainException.ThrowIfNullOrWhiteSpace(avatar, "Avatar");
        Avatar = avatar;
        events.Raise(new VenueChangedDomainEvent(this));
    }

    public void UpdateLocation(Point location, Address address)
    {
        DomainException.ThrowIfNull(location, "Location");
        if (address is null || string.IsNullOrWhiteSpace(address.County) || string.IsNullOrWhiteSpace(address.Town))
            throw new DomainException("County and Town are required.");
        Location = location;
        Address = address;
        events.Raise(new VenueChangedDomainEvent(this));
    }

    public void UpdateEmail(string email)
    {
        DomainException.ThrowIfNullOrWhiteSpace(email, "Email");
        Email = email;
        events.Raise(new VenueChangedDomainEvent(this));
    }

    private static void Validate(string name, string about, string bannerUrl, string avatar, Point location, Address address, string email)
    {
        DomainException.ThrowIfNullOrWhiteSpace(name, "Name");
        DomainException.ThrowIfNullOrWhiteSpace(about, "About");
        DomainException.ThrowIfNullOrWhiteSpace(bannerUrl, "Banner URL");
        DomainException.ThrowIfNullOrWhiteSpace(avatar, "Avatar");
        DomainException.ThrowIfNull(location, "Location");
        if (address is null || string.IsNullOrWhiteSpace(address.County) || string.IsNullOrWhiteSpace(address.Town))
            throw new DomainException("County and Town are required.");
        DomainException.ThrowIfNullOrWhiteSpace(email, "Email");
    }
}
