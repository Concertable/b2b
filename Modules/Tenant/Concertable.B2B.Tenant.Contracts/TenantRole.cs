namespace Concertable.B2B.Tenant.Contracts;

/// <summary>
/// A tenant membership's role — a predefined bundle of permissions (the bundle lives in the permission
/// catalog, resolved in code). Exactly one role per membership. Every tenant keeps at least one Owner.
/// </summary>
public enum TenantRole
{
    Owner = 1,
    Manager = 2,
    Finance = 3,
    Staff = 4,
    Door = 5,
    Sound = 6,
}
