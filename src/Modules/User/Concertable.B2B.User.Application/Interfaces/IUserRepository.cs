
using Concertable.DataAccess.Application;

namespace Concertable.B2B.User.Application.Interfaces;

internal interface IUserRepository : IGuidRepository<UserEntity>
{
    Task<bool> ExistsByEmailAsync(string email);
    Task<UserEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<UserEntity>> GetByIdsAsync(IEnumerable<Guid> ids);
}
