using Concertable.User.Contracts;
using NetTopologySuite.Geometries;

namespace Concertable.User.Domain;

public class UserEntity : IGuidEntity
{
    protected UserEntity() { }

    private UserEntity(Guid id, string email, Role role)
    {
        Id = id;
        Email = email;
        Role = role;
    }

    public Guid Id { get; private set; }
    public string Email { get; private set; } = null!;
    public Role Role { get; private set; }
    public Address? Address { get; private set; }
    public Point? Location { get; private set; }
    public string Avatar { get; private set; } = string.Empty;

    public static UserEntity FromRegistration(Guid id, string email, Role role) =>
        new(id, email, role);

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
