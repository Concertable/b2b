namespace Concertable.B2B.Authorization.Contracts;

/// <summary>How wide a permission reaches. Ordering is significant: a wider audience sorts above a narrower
/// one where several sources of the same permission are combined.</summary>
public enum ResourceAudience
{
    None = 0,
    AssignedResources = 1,
    TenantResources = 2,
}
