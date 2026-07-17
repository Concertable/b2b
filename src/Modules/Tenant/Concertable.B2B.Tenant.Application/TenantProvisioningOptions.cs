namespace Concertable.B2B.Tenant.Application;

/// <summary>
/// Platform-level provisioning defaults, bound from the <c>Tenant</c> config section. Holds the
/// <see cref="Jurisdiction"/> a newly-registered tenant is provisioned with, so the value is config-sourced
/// rather than a literal at the provisioning call site. Defaults to <see cref="Jurisdiction.Gb"/> — the only
/// launch jurisdiction — and is overridden per environment once we onboard sellers elsewhere.
/// </summary>
public sealed class TenantProvisioningOptions
{
    public Jurisdiction DefaultJurisdiction { get; set; } = Jurisdiction.Gb;
}
