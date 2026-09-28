namespace Concertable.B2B.Tenant.Contracts;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class RequiresBusinessActivityAttribute : Attribute
{
    public RequiresBusinessActivityAttribute(TenantBusinessActivityKind kind) => Kind = kind;

    public TenantBusinessActivityKind Kind { get; }
}
