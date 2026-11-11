using Concertable.B2B.User.Domain.Entities;
using Concertable.Seed.Identity;

namespace Concertable.B2B.IntegrationTests.Fixtures;

public static class UserEntityExtensions
{
    extension(UserEntity user)
    {
        public Guid TenantId => TenantSeedIds.For(user.Id);
    }
}
