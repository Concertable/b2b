namespace Concertable.B2B.User.Infrastructure;

internal sealed class UserModule : IUserModule
{
    private readonly IUserRepository userRepository;
    private readonly IUserMapper userMapper;

    public UserModule(IUserRepository userRepository, IUserMapper userMapper)
    {
        this.userRepository = userRepository;
        this.userMapper = userMapper;
    }

    public async Task<UserDto?> GetByIdAsync(Guid id)
    {
        var user = await userRepository.GetByIdAsync(id);
        return user is null ? null : await userMapper.ToDtoAsync(user);
    }

    public async Task<IReadOnlyCollection<UserDto>> GetByIdsAsync(IEnumerable<Guid> ids)
    {
        var users = await userRepository.GetByIdsAsync(ids);
        var result = new List<UserDto>(users.Count);
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

    public async Task<ManagerDto?> GetManagerByIdAsync(Guid userId)
    {
        var user = await userRepository.GetByIdAsync(userId);
        return user is null ? null : new ManagerDto { Id = user.Id, Email = user.Email, Avatar = user.Avatar };
    }
}
