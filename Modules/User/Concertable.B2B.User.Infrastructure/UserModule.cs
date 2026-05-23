using Concertable.B2B.User.Infrastructure.Data;
using Concertable.B2B.User.Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.User.Infrastructure;

internal class UserModule : IUserModule
{
    private readonly UserDbContext context;
    private readonly IUserRepository userRepository;
    private readonly IUserMapper userMapper;

    public UserModule(UserDbContext context, IUserRepository userRepository, IUserMapper userMapper)
    {
        this.context = context;
        this.userRepository = userRepository;
        this.userMapper = userMapper;
    }

    public async Task<IUser?> GetByIdAsync(Guid id)
    {
        var user = await userRepository.GetByIdAsync(id);
        return user is null ? null : await userMapper.ToDtoAsync(user);
    }

    public async Task<IReadOnlyCollection<IUser>> GetByIdsAsync(IEnumerable<Guid> ids)
    {
        var users = await userRepository.GetByIdsAsync(ids);
        var result = new List<IUser>(users.Count);
        foreach (var user in users)
        {
            var dto = await userMapper.ToDtoAsync(user);
            if (dto is not null)
                result.Add(dto);
        }
        return result;
    }

    public async Task<ManagerDto?> GetManagerByIdAsync(Guid userId)
    {
        var isManager = await context.VenueManagerProfiles.AnyAsync(p => p.Sub == userId)
            || await context.ArtistManagerProfiles.AnyAsync(p => p.Sub == userId)
            || await context.AdminProfiles.AnyAsync(p => p.Sub == userId);

        if (!isManager) return null;

        var user = await userRepository.GetByIdAsync(userId);
        return user is null ? null : new ManagerDto { Id = user.Id, Email = user.Email, Avatar = user.Avatar };
    }
}
