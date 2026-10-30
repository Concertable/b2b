namespace Concertable.B2B.Authorization.Contracts;

public static class PermissionResourceBindingExtensions
{
    extension(TenantPermission permission)
    {
        public PermissionResourceBinding? BindingFor(string resource, string? facet) =>
            AuthorizationCatalog.Permissions.TryGetValue(permission, out var descriptor)
                ? descriptor.ResourceBindings.SingleOrDefault(binding =>
                    string.Equals(binding.Resource, resource, StringComparison.Ordinal)
                    && binding.Facet == facet)
                : null;
    }
}
