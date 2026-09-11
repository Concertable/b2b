using System.ComponentModel;
using Concertable.B2B.Venue.Contracts;
using Concertable.Kernel;
using Concertable.B2B.Venue.Domain.Events;
using NetTopologySuite.Geometries;

namespace Concertable.B2B.Venue.Domain.Entities;

[DisplayName(DisplayNames.Venue)]
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
    public Point Location { get; private set; } = null!;
    public Address Address { get; private set; } = null!;
    public string Avatar { get; private set; } = null!;
    public string Email { get; private set; } = null!;

    public IReadOnlyList<IDomainEvent> DomainEvents => events.DomainEvents;
    public void ClearDomainEvents() => events.Clear();

    public static Result<VenueEntity, ValidationErrors> Create(
        Guid userId,
        string name,
        string about,
        string bannerUrl,
        string avatar,
        Point location,
        Address address,
        string email)
    {
        var validation = ValidateProfile(name, about);
        return validation.Bind(() =>
        {
            ValidateCollaborators(bannerUrl, avatar, location, address, email);

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
            return Result.Success<VenueEntity, ValidationErrors>(venue);
        });
    }

    public UnitResult<ValidationErrors> Update(string name, string about, string bannerUrl)
    {
        var validation = ValidateProfile(name, about);
        if (validation.IsFailure)
            return validation;

        ValidateCollaborators(bannerUrl, Avatar, Location, Address, Email);
        Name = name;
        About = about;
        BannerUrl = bannerUrl;
        events.Raise(new VenueChangedDomainEvent(this));
        return new Success();
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

    public static UnitResult<ValidationErrors> ValidateProfile(string name, string about)
    {
        var errors = new List<KeyValuePair<string, string>>();

        if (string.IsNullOrWhiteSpace(name))
            errors.Add(new(nameof(Name), "Name is required."));
        else if (name.Length > 100)
            errors.Add(new(nameof(Name), "Name must be 100 characters or fewer."));

        if (string.IsNullOrWhiteSpace(about))
            errors.Add(new(nameof(About), "About is required."));
        else if (about.Length > 1000)
            errors.Add(new(nameof(About), "About must be 1000 characters or fewer."));

        return errors.Count == 0
            ? new Success()
            : new ValidationErrors(errors);
    }

    private static void ValidateCollaborators(string bannerUrl, string avatar, Point location, Address address, string email)
    {
        DomainException.ThrowIfNullOrWhiteSpace(bannerUrl, "Banner URL");
        DomainException.ThrowIfNullOrWhiteSpace(avatar, "Avatar");
        DomainException.ThrowIfNull(location, "Location");
        if (address is null || string.IsNullOrWhiteSpace(address.County) || string.IsNullOrWhiteSpace(address.Town))
            throw new DomainException("County and Town are required.");
        DomainException.ThrowIfNullOrWhiteSpace(email, "Email");
    }
}
