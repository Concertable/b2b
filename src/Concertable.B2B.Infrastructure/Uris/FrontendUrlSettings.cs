using Concertable.B2B.Tenant.Contracts.Enums;

namespace Concertable.B2B.Infrastructure.Uris;

internal sealed class FrontendUrlSettings
{
    public Dictionary<TenantType, string> Frontends { get; set; } = new();
}
