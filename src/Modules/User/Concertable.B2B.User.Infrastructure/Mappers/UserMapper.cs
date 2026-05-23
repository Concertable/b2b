using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.User.Infrastructure.Mappers;

internal sealed class UserMapper : IUserMapper
{
    private readonly IKeyedServiceProvider keyedServiceProvider;

    public UserMapper(IKeyedServiceProvider keyedServiceProvider)
    {
        this.keyedServiceProvider = keyedServiceProvider;
    }

    public async Task<IUser?> ToDtoAsync(UserEntity user)
    {
        var mapper = keyedServiceProvider.GetKeyedService<IRoleMapper>(user.Role);
        if (mapper is null)
            return null;
        return await mapper.ToDtoAsync(user);
    }
}
