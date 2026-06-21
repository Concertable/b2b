namespace Concertable.B2B.Tenant.Contracts;

/// <summary>
/// The default persona for every <c>[HasPermission]</c> on a controller (or action) that doesn't name one
/// itself — so a single-persona controller declares its persona once instead of repeating it on each
/// endpoint. An explicit <c>[HasPermission(perm, persona)]</c> still wins. Omit it from a persona-agnostic
/// controller (e.g. payouts), where the absence of a persona is the point.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class TenantPersonaAttribute : Attribute
{
    public TenantPersonaAttribute(TenantType persona) => Persona = persona;

    public TenantType Persona { get; }
}
