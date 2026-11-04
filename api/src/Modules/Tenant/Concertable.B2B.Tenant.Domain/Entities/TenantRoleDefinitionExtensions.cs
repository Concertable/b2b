using Concertable.B2B.Authorization.Contracts;

namespace Concertable.B2B.Tenant.Domain.Entities;

public static class TenantRoleDefinitionExtensions
{
    extension(IEnumerable<TenantRoleDefinition> roles)
    {
        public PermissionSet ToPermissionSet() =>
            PermissionSet.From(roles
                .SelectMany(role => role.Permissions)
                .Where(row => row.Access is ResourceAccess.Assigned or ResourceAccess.All)
                .Select(row => TenantPermission.TryParseName(row.PermissionKey, out var permission)
                    ? KeyValuePair.Create(permission, row.Access)
                    : default)
                .Where(grant => grant.Key != default));
    }
}
