namespace Concertable.B2B.DataAccess.Application;

public sealed class MembershipAuthority
{
    public Guid MembershipId { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid UserId { get; private set; }
    public long PermissionVersion { get; private set; }
}
