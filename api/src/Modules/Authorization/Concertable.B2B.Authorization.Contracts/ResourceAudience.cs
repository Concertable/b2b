namespace Concertable.B2B.Authorization.Contracts;

public enum ResourceAudience
{
    // Ordering is significant; broader audiences must have larger values.
    None = 0,
    AssignedResources = 1,
    TenantResources = 2,
}
