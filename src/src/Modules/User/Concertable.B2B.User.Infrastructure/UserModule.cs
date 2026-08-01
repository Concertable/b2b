using Concertable.B2B.User.Infrastructure.Data;
using Concertable.B2B.User.Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.User.Infrastructure;

internal sealed class UserModule : IUserModule
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

    public async Task<Option<UserBase>> GetByIdAsync(Guid id)
    {
        var user = await userRepository.GetByIdAsync(id);
        return user is null
            ? Option.None<UserBase>()
            : (await userMapper.ToDtoAsync(user)).ToOption();
    }

    public async Task<IReadOnlyList<UserBase>> GetByIdsAsync(IEnumerable<Guid> ids)
    {
        var users = await userRepository.GetByIdsAsync(ids);
        var result = new List<UserBase>(users.Count);
        foreach (var user in users)
        {
            var dto = await userMapper.ToDtoAsync(user);
            if (dto is not null)
                result.Add(dto);
        }
        return result;
    }

    public async Task<IReadOnlyDictionary<Guid, string>> GetEmailsByIdsAsync(IEnumerable<Guid> ids)
    {
        var users = await userRepository.GetByIdsAsync(ids);
        return users.ToDictionary(u => u.Id, u => u.Email);
    }

    public async Task<Option<ManagerDto>> GetManagerByIdAsync(Guid userId)
    {
        var isManager = await context.VenueManagerProfiles.AnyAsync(p => p.Sub == userId)
            || await context.ArtistManagerProfiles.AnyAsync(p => p.Sub == userId)
            || await context.AdminProfiles.AnyAsync(p => p.Sub == userId);

        if (!isManager)
            return Option.None<ManagerDto>();

        var user = await userRepository.GetByIdAsync(userId);
        return user is null
            ? Option.None<ManagerDto>()
            : Option.Some(new ManagerDto { Id = user.Id, Email = user.Email, Avatar = user.Avatar });
    }
}
