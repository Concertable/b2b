namespace Concertable.B2B.Tenant.Contracts;

/// <summary>
/// Marks an endpoint as belonging to one kind of marketplace work: the active tenant must have that business
/// profile activated. This is eligibility, not authority — the caller's role still has to grant the
/// permission, and a tenant holding the profile still needs a grant on the particular resource. A tenant may
/// hold several profiles, so this never excludes a business for being "the other type".
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class RequiresBusinessProfileAttribute : Attribute
{
    public RequiresBusinessProfileAttribute(TenantBusinessProfileKind kind) => Kind = kind;

    public TenantBusinessProfileKind Kind { get; }
}
