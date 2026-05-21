namespace Concertable.User.Infrastructure.Data;

internal sealed class PasswordResetTokenEntity : IIdEntity
{
    private PasswordResetTokenEntity() { }

    public int Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Token { get; private set; } = null!;
    public DateTime Expires { get; private set; }

    public bool IsActive => DateTime.UtcNow < Expires;

    public static PasswordResetTokenEntity Create(Guid userId, string token, DateTime expires) => new()
    {
        UserId = userId,
        Token = token,
        Expires = expires
    };
}
