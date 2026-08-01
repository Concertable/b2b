using Concertable.B2B.User.Domain.Entities;
using Concertable.Kernel.ValueObjects;
using NetTopologySuite.Geometries;

namespace Concertable.B2B.Seed.Infrastructure.Factories;

public static class UserFactory
{
    public static UserEntity FromRegistration(Guid id, string email) =>
        UserEntity.FromRegistration(id, email);

    public static UserEntity FromRegistration(Guid id, string email, Point location, Address address, string avatar)
    {
        var user = UserEntity.FromRegistration(id, email);
        user.UpdateLocation(location, address);
        user.UpdateAvatar(avatar);
        return user;
    }
}
