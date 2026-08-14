using Concertable.B2B.User.Application.Interfaces;

namespace Concertable.B2B.User.Infrastructure;

internal sealed class UserModule : IUserModule
{
    private readonly IUserService userService;

    public UserModule(IUserService userService)
    {
        this.userService = userService;
    }

    public Task<Option<UserDto>> GetByIdAsync(Guid id) =>
        userService.GetByIdAsync(id);

    public Task<IReadOnlyList<UserDto>> GetByIdsAsync(IEnumerable<Guid> ids) =>
        userService.GetByIdsAsync(ids);

    public Task<IReadOnlyDictionary<Guid, string>> GetEmailsByIdsAsync(IEnumerable<Guid> ids) =>
        userService.GetEmailsByIdsAsync(ids);

    public Task<Option<ManagerDto>> GetManagerByIdAsync(Guid userId) =>
        userService.GetManagerByIdAsync(userId);
}
