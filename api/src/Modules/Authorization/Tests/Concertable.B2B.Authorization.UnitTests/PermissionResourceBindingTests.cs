using Concertable.B2B.Authorization.Contracts;

namespace Concertable.B2B.Authorization.UnitTests;

public sealed class PermissionResourceBindingTests
{
    [Fact]
    public void BindingFor_PermissionWithSeveralScopes_RequiresTheExactScope()
    {
        Assert.Null(TenantPermission.OperationsView.BindingFor(AuthorizationResource.Booking, null));

        var operations = TenantPermission.OperationsView.BindingFor(AuthorizationResource.Booking, "Operations");

        Assert.NotNull(operations);
        Assert.Equal(ResourcePolicy.BookingGrant, operations.Policy);
        Assert.Equal<string>(["Operations"], operations.RequiresScopes);
    }

    [Fact]
    public void BindingFor_UnboundResourceOrScope_ReturnsNull()
    {
        Assert.Null(TenantPermission.OperationsView.BindingFor(AuthorizationResource.Invoice, "Read"));
        Assert.Null(TenantPermission.OperationsView.BindingFor(AuthorizationResource.Booking, "Finance"));
        Assert.Null(default(TenantPermission).BindingFor(AuthorizationResource.Booking, "Summary"));
    }

    [Theory]
    [InlineData(AuthorizationResource.Concert)]
    [InlineData(AuthorizationResource.Conversation)]
    public void BindingFor_ShareBindingHasNoScope(AuthorizationResource resource)
    {
        var binding = TenantPermission.ResourcesShare.BindingFor(resource, null);

        Assert.NotNull(binding);
        Assert.Equal(ResourcePolicy.PrincipalAdministration, binding.Policy);
        Assert.Empty(binding.RequiresScopes);
    }

    [Fact]
    public void Catalog_ResourceBindingsAreUniquePerResourceAndScope()
    {
        foreach (var descriptor in AuthorizationCatalog.Permissions.Values)
        {
            var keys = descriptor.ResourceBindings.Select(binding => (binding.Resource, binding.Scope)).ToList();
            Assert.Equal(keys.Count, keys.Distinct().Count());
        }
    }

    [Fact]
    public void Catalog_EveryBindingNamesADeclaredResource()
    {
        var bindings = AuthorizationCatalog.Permissions.Values.SelectMany(descriptor => descriptor.ResourceBindings);

        Assert.All(bindings, binding => Assert.True(Enum.IsDefined(binding.Resource)));
    }

    [Theory]
    [InlineData(AuthorizationResource.Tenant, "tenant")]
    [InlineData(AuthorizationResource.Conversation, "conversation")]
    [InlineData(AuthorizationResource.Contract, "contract")]
    public void WireName_IsTheCatalogResourceName(AuthorizationResource resource, string expected)
    {
        Assert.Equal(expected, resource.WireName);
    }
}
