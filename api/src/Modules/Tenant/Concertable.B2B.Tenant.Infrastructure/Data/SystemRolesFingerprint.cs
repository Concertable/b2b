namespace Concertable.B2B.Tenant.Infrastructure.Data;

internal sealed class SystemRolesFingerprint
{
    private const int SingletonId = 1;

    private SystemRolesFingerprint() { }

    public int Id { get; private set; }
    public Sha256Digest Value { get; private set; } = null!;

    public static SystemRolesFingerprint Create(Sha256Digest value) => new() { Id = SingletonId, Value = value };

    public void Replace(Sha256Digest value) => Value = value;
}
