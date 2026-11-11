using Concertable.B2B.Authorization.Contracts;

namespace Concertable.B2B.Tenant.Infrastructure.Data;

internal static class TenantRoleProvisioning
{
    public static IReadOnlyList<TenantRoleDefinition> CreatePresets(Guid tenantId) =>
        AuthorizationCatalog.Presets.Values
            .Select(preset => TenantRoleDefinition.CreateSystemPreset(tenantId, preset.Key))
            .ToArray();
}
