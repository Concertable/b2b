using Concertable.User.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.User.Infrastructure.Repositories;

internal class UserRepository : IUserRepository
{
    private readonly UserDbContext context;

    public UserRepository(UserDbContext context)
    {
        this.context = context;
    }

    public async Task<UserEntity> AddAsync(UserEntity entity)
    {
        await context.Users.AddAsync(entity);
        return entity;
    }

    public async Task<IEnumerable<UserEntity>> AddRangeAsync(IEnumerable<UserEntity> entities)
    {
        await context.Users.AddRangeAsync(entities);
        return entities;
    }

    public async Task<IEnumerable<UserEntity>> GetAllAsync() =>
        await context.Users.ToListAsync();

    public void Remove(UserEntity entity) => context.Users.Remove(entity);

    public void Update(UserEntity entity) => context.Users.Update(entity);

    public async Task SaveChangesAsync() => await context.SaveChangesAsync();

    public async Task<UserEntity?> GetByIdAsync(Guid id) =>
        await context.Users.FindAsync(id);

    public bool Exists(Guid id) => context.Users.Any(e => e.Id == id);

    public Task<bool> ExistsByEmailAsync(string email) =>
        context.Users.AnyAsync(u => u.Email == email);

    public Task<UserEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public async Task<IReadOnlyCollection<UserEntity>> GetByIdsAsync(IEnumerable<Guid> ids)
    {
        return await context.Users.Where(u => ids.Contains(u.Id)).ToListAsync();
    }
}
