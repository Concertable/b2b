using Concertable.Kernel;
using NetTopologySuite.Geometries;

namespace Concertable.B2B.User.Domain.Entities;

public sealed class UserEntity : IGuidEntity
{
    protected UserEntity() { }

    private UserEntity(Guid id, string email)
    {
        Id = id;
        Email = email;
    }

    public Guid Id { get; private set; }
    public string Email { get; private set; } = null!;
    public Address? Address { get; private set; }
    public Point? Location { get; private set; }
    public string? Avatar { get; private set; }

    public static UserEntity FromRegistration(Guid id, string email) =>
        new(id, email);

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
