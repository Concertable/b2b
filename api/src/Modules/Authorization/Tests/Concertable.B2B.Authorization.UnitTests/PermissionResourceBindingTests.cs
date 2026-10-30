using Concertable.B2B.Authorization.Contracts;

namespace Concertable.B2B.Authorization.UnitTests;

public sealed class PermissionResourceBindingTests
{
    [Fact]
    public void BindingFor_PermissionWithSeveralFacets_RequiresTheExactFacet()
    {
        Assert.Null(TenantPermission.OperationsView.BindingFor("booking", null));

        var operations = TenantPermission.OperationsView.BindingFor("booking", "Operations");

        Assert.NotNull(operations);
        Assert.Equal(ResourcePolicy.BookingGrant, operations.Policy);
        Assert.Equal<string>(["Operations"], operations.RequiresScopes);
    }

    [Fact]
    public void BindingFor_UnboundResourceOrFacet_ReturnsNull()
    {
        Assert.Null(TenantPermission.OperationsView.BindingFor("invoice", "Read"));
        Assert.Null(TenantPermission.OperationsView.BindingFor("booking", "Finance"));
        Assert.Null(default(TenantPermission).BindingFor("booking", "Summary"));
    }

    [Theory]
    [InlineData("concert")]
    [InlineData("conversation")]
    public void BindingFor_ShareBindingHasNoFacet(string resource)
    {
        var binding = TenantPermission.ResourcesShare.BindingFor(resource, null);

        Assert.NotNull(binding);
        Assert.Equal(ResourcePolicy.PrincipalAdministration, binding.Policy);
        Assert.Empty(binding.RequiresScopes);
    }

    [Fact]
    public void Catalog_ResourceBindingsAreUniquePerResourceAndFacet()
    {
        foreach (var descriptor in AuthorizationCatalog.Permissions.Values)
        {
            var keys = descriptor.ResourceBindings.Select(binding => (binding.Resource, binding.Facet)).ToList();
            Assert.Equal(keys.Count, keys.Distinct().Count());
        }
    }
}
