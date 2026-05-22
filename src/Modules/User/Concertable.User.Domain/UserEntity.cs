using Concertable.User.Contracts;
using Concertable.User.Domain.Events;
using NetTopologySuite.Geometries;

namespace Concertable.User.Domain;

public class UserEntity : IGuidEntity, IEventRaiser
{
    private readonly EventRaiser _events = new();

    protected UserEntity() { }

    private UserEntity(string email, string passwordHash, Role role)
    {
        Email = email;
        PasswordHash = passwordHash;
        Role = role;
        _events.Raise(new UserCreatedDomainEvent(this));
    }

    public Guid Id { get; private set; }
    public string Email { get; private set; } = null!;
    public string PasswordHash { get; set; } = string.Empty;
    public Role Role { get; private set; }
    public Address? Address { get; private set; }
    public Point? Location { get; private set; }
    public string Avatar { get; private set; } = string.Empty;
    public bool IsEmailVerified { get; private set; }

    public IReadOnlyList<IDomainEvent> DomainEvents => _events.DomainEvents;
    public void ClearDomainEvents() => _events.Clear();

    public static UserEntity Create(string email, string passwordHash, Role role) =>
        new(email, passwordHash, role);

    public void VerifyEmail() => IsEmailVerified = true;

    public void UpdateLocation(Point location, Address? address = null)
    {
        Location = location;
        Address = address;
    }

    public void UpdateAvatar(string avatar)
    {
        Avatar = avatar;
    }

    public void SyncFromManager(string avatar, Point location, Address address)
    {
        Avatar = avatar;
        Location = location;
        Address = address;
    }
}
