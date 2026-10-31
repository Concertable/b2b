namespace Concertable.B2B.Authorization.Contracts;

public static class PermissionResourceBindingExtensions
{
    extension(TenantPermission permission)
    {
        public PermissionResourceBinding? FindBinding(AuthorizationResource resource, string? scope) =>
            AuthorizationCatalog.Permissions.TryGetValue(permission, out var descriptor)
                ? descriptor.ResourceBindings.SingleOrDefault(binding =>
                    binding.Resource == resource && binding.Scope == scope)
                : null;
    }
}
