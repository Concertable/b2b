
using Concertable.B2B.User.Application.Errors;
using Concertable.B2B.User.Contracts;

namespace Concertable.B2B.User.Application.Interfaces;

internal interface IUserService
{
    Task<Result<UserDto, SaveLocationError>> SaveLocationAsync(double latitude, double longitude);
    Task<Option<UserDto>> GetByIdAsync(Guid id);
    Task<IReadOnlyList<UserDto>> GetByIdsAsync(IEnumerable<Guid> ids);
    Task<IReadOnlyDictionary<Guid, string>> GetEmailsByIdsAsync(IEnumerable<Guid> ids);
    Task<Option<ManagerDto>> GetManagerByIdAsync(Guid userId);
    Task<Option<Guid>> GetIdByEmailAsync(string email);
}
