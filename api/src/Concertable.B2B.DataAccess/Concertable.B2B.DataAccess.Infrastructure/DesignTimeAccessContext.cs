namespace Concertable.B2B.DataAccess.Infrastructure;

// Design-time only builds the model; no query ever evaluates the access filter.
public sealed class DesignTimeAccessContext : IAccessContext
{
    public static readonly DesignTimeAccessContext Instance = new();

    public Guid? TenantId => null;

    public Guid? UserId => null;

    public long? AuthorizationVersion => null;

    public bool IsHost => true;

    public DateTime UtcNow => DateTime.UnixEpoch;
}
