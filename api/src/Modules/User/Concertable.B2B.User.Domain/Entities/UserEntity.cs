using Concertable.Kernel;
using NetTopologySuite.Geometries;

namespace Concertable.B2B.User.Domain.Entities;

public sealed class UserEntity : IGuidEntity
{
    private UserEntity() { }

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

    public static UserEntity Create(Guid id, string email) =>
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

    /// <summary>Anonymises the natural person for GDPR erasure (art. 17): the personal identifiers are scrubbed,
    /// while the row and its <see cref="Id"/> (the Auth <c>sub</c>) survive so downstream foreign keys stay valid.</summary>
    public void Anonymise(string tombstoneEmail)
    {
        Email = tombstoneEmail;
        Address = null;
        Location = null;
        Avatar = null;
    }
}
