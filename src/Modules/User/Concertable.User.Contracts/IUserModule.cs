namespace Concertable.User.Contracts;

public record UserCredentials(Guid Id, string Email, string PasswordHash, bool IsEmailVerified);

public interface IUserModule
{
    Task<IUser?> GetByIdAsync(Guid id);
    Task<IReadOnlyCollection<IUser>> GetByIdsAsync(IEnumerable<Guid> ids);
    Task<ManagerDto?> GetManagerByIdAsync(Guid userId);

    Task<bool> CustomerEmailExistsAsync(string email, CancellationToken ct = default);
    Task<bool> VenueManagerEmailExistsAsync(string email, CancellationToken ct = default);
    Task<bool> ArtistManagerEmailExistsAsync(string email, CancellationToken ct = default);

    Task CreateCustomerAsync(string email, string passwordHash, CancellationToken ct = default);
    Task CreateVenueManagerAsync(string email, string passwordHash, CancellationToken ct = default);
    Task CreateArtistManagerAsync(string email, string passwordHash, CancellationToken ct = default);

    Task<UserCredentials?> GetCredentialsByEmailAsync(string email, CancellationToken ct = default);
    Task<UserCredentials?> GetCredentialsByIdAsync(Guid userId, CancellationToken ct = default);
    Task SetEmailVerifiedAsync(Guid userId, CancellationToken ct = default);
    Task SetPasswordHashAsync(Guid userId, string newHash, CancellationToken ct = default);

    Task<string?> CreateEmailVerificationTokenAsync(Guid userId, CancellationToken ct = default);
    Task<bool> VerifyEmailWithTokenAsync(string token, CancellationToken ct = default);
    Task<string?> CreatePasswordResetTokenAsync(string email, CancellationToken ct = default);
    Task<bool> ResetPasswordWithTokenAsync(string token, string newPasswordHash, CancellationToken ct = default);
}
