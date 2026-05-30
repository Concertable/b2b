using Concertable.B2B.User.Domain;
using Concertable.Kernel;
using Concertable.Kernel.Identity;
using NetTopologySuite.Geometries;

namespace Concertable.B2B.Seed.Infrastructure.Factories;

public static class UserFactory
{
    public static UserEntity FromRegistration(Guid id, string email, Role role) =>
        UserEntity.FromRegistration(id, email, role);

    public static UserEntity FromRegistration(Guid id, string email, Role role, Point location, Address address, string avatar)
    {
        var user = UserEntity.FromRegistration(id, email, role);
        user.UpdateLocation(location, address);
        user.UpdateAvatar(avatar);
        return user;
    }
}
