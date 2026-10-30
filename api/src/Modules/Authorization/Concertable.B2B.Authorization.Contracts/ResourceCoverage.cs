namespace Concertable.B2B.Authorization.Contracts;

public enum ResourceCoverage
{
    // Ordering is significant; broader coverage must have a larger value.
    None = 0,
    AssignedResources = 1,
    TenantResources = 2,
}
