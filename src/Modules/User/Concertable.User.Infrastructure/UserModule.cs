using Concertable.User.Infrastructure.Data;
using Concertable.User.Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace Concertable.User.Infrastructure;

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

    public Task<bool> CustomerEmailExistsAsync(string email, CancellationToken ct = default) =>
        context.Users.WhereCredentials(email, Role.Customer).AnyAsync(ct);

    public Task<bool> VenueManagerEmailExistsAsync(string email, CancellationToken ct = default) =>
        context.Users.WhereCredentials(email, Role.VenueManager).AnyAsync(ct);

    public Task<bool> ArtistManagerEmailExistsAsync(string email, CancellationToken ct = default) =>
        context.Users.WhereCredentials(email, Role.ArtistManager).AnyAsync(ct);

    public async Task CreateCustomerAsync(string email, string passwordHash, CancellationToken ct = default)
    {
        var user = UserEntity.Create(email, passwordHash, Role.Customer);
        context.Users.Add(user);
        await context.SaveChangesAsync(ct);
    }

    public async Task CreateVenueManagerAsync(string email, string passwordHash, CancellationToken ct = default)
    {
        var user = UserEntity.Create(email, passwordHash, Role.VenueManager);
        context.Users.Add(user);
        context.VenueManagerProfiles.Add(new VenueManagerProfileEntity(user.Id));
        await context.SaveChangesAsync(ct);
    }

    public async Task CreateArtistManagerAsync(string email, string passwordHash, CancellationToken ct = default)
    {
        var user = UserEntity.Create(email, passwordHash, Role.ArtistManager);
        context.Users.Add(user);
        context.ArtistManagerProfiles.Add(new ArtistManagerProfileEntity(user.Id));
        await context.SaveChangesAsync(ct);
    }

    public async Task<UserCredentials?> GetCredentialsByEmailAsync(string email, CancellationToken ct = default)
    {
        var user = await context.Users.Where(u => u.Email == email).FirstOrDefaultAsync(ct);
        return user is null ? null : new UserCredentials(user.Id, user.Email, user.PasswordHash, user.IsEmailVerified);
    }

    public async Task<UserCredentials?> GetCredentialsByIdAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        return user is null ? null : new UserCredentials(user.Id, user.Email, user.PasswordHash, user.IsEmailVerified);
    }

    public async Task SetEmailVerifiedAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null) return;
        user.VerifyEmail();
        await context.SaveChangesAsync(ct);
    }

    public async Task SetPasswordHashAsync(Guid userId, string newHash, CancellationToken ct = default)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null) return;
        user.PasswordHash = newHash;
        await context.SaveChangesAsync(ct);
    }

    public async Task<string?> CreateEmailVerificationTokenAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null || user.IsEmailVerified) return null;

        var token = GenerateToken();
        var entity = EmailVerificationTokenEntity.Create(userId, token, DateTime.UtcNow.AddHours(24));
        context.EmailVerificationTokens.Add(entity);
        await context.SaveChangesAsync(ct);
        return token;
    }

    public async Task<bool> VerifyEmailWithTokenAsync(string token, CancellationToken ct = default)
    {
        var entity = await context.EmailVerificationTokens.FirstOrDefaultAsync(t => t.Token == token, ct);
        if (entity is null || !entity.IsActive) return false;

        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == entity.UserId, ct);
        if (user is null) return false;

        user.VerifyEmail();
        context.EmailVerificationTokens.Remove(entity);
        await context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<string?> CreatePasswordResetTokenAsync(string email, CancellationToken ct = default)
    {
        var user = await context.Users.Where(u => u.Email == email).FirstOrDefaultAsync(ct);
        if (user is null) return null;

        var token = GenerateToken();
        var entity = PasswordResetTokenEntity.Create(user.Id, token, DateTime.UtcNow.AddHours(1));
        context.PasswordResetTokens.Add(entity);
        await context.SaveChangesAsync(ct);
        return token;
    }

    public async Task<bool> ResetPasswordWithTokenAsync(string token, string newPasswordHash, CancellationToken ct = default)
    {
        var entity = await context.PasswordResetTokens.FirstOrDefaultAsync(t => t.Token == token, ct);
        if (entity is null || !entity.IsActive) return false;

        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == entity.UserId, ct);
        if (user is null) return false;

        user.PasswordHash = newPasswordHash;
        context.PasswordResetTokens.Remove(entity);
        await context.SaveChangesAsync(ct);
        return true;
    }

    private static string GenerateToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
}
