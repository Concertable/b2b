using Concertable.B2B.User.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.User.Infrastructure.Repositories;

internal sealed class UserRepository : Repository<UserEntity>, IUserRepository
{
    public UserRepository(UserDbContext context) : base(context) { }

    public Task<bool> ExistsByEmailAsync(string email) =>
        context.Users.AnyAsync(u => u.Email == email);

    public async Task<IReadOnlyCollection<UserEntity>> GetByIdsAsync(IEnumerable<Guid> ids) =>
        await context.Users.Where(u => ids.Contains(u.Id)).ToListAsync();
}
