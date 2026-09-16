namespace Concertable.B2B.Tenant.Contracts.Enums;

/// <summary>
/// A kind of marketplace work a tenant has activated. A tenant holds zero or more of these: an agency or
/// production business legitimately holds none, and a business that both operates a room and performs holds
/// two. A profile is eligibility for particular work, never the tenant's authority or its identity in an
/// agreement already made.
/// </summary>
public enum TenantBusinessProfileKind
{
    VenueOperator = 1,
    Artist = 2,
    Promoter = 3,
}
