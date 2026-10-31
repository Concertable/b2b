using Concertable.B2B.Authorization.Contracts;

namespace Concertable.B2B.Tenant.Domain.Entities;

public static class TenantRoleDefinitionExtensions
{
    extension(IEnumerable<TenantRoleDefinition> roles)
    {
        public EffectivePermissions ToEffectivePermissions() =>
            EffectivePermissions.From(roles
                .SelectMany(role => role.Permissions)
                .Where(row => row.Audience is ResourceAccess.Assigned or ResourceAccess.All)
                .Select(row => TenantPermission.TryParse(row.PermissionKey, out var permission)
                    ? KeyValuePair.Create(permission, row.Audience)
                    : default)
                .Where(grant => grant.Key != default));
    }
}
