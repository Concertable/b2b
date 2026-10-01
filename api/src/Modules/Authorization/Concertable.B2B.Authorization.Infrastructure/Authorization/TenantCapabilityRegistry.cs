using Concertable.B2B.Authorization.Contracts;

namespace Concertable.B2B.Authorization.Infrastructure.Authorization;

internal sealed class TenantCapabilityRegistry
{
    private readonly IReadOnlySet<TenantPermission> permissions;

    public TenantCapabilityRegistry(IEnumerable<PermissionDescriptor> descriptors)
    {
        var supported = new HashSet<TenantPermission>();
        foreach (var descriptor in descriptors)
        {
            if (!TenantPermission.All.Contains(descriptor.Permission))
                throw new InvalidOperationException("An unknown tenant permission is registered.");

            foreach (var binding in descriptor.ResourceBindings.Where(binding => binding.Resource == "tenant"))
            {
                if (binding.Facet is not null
                    || binding.Policy != "membership"
                    || binding.RequiresScopes is null
                    || binding.RequiresScopes.Count != 0
                    || !supported.Add(descriptor.Permission))
                    throw new InvalidOperationException("An invalid tenant capability is registered.");
            }
        }

        permissions = supported;
    }

    public bool Contains(TenantPermission permission) => permissions.Contains(permission);
}
