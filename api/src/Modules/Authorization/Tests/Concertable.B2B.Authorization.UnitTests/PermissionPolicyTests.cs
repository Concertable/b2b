namespace Concertable.B2B.Authorization.UnitTests;

public sealed class PermissionPolicyTests
{
    [Fact]
    public void Name_DeclaredPermission_ReturnsPolicyName()
    {
        var policyName = PermissionPolicy.Name(TenantPermission.OperationsView);

        Assert.Equal("perm:operations:view", policyName);
    }

    [Fact]
    public void Name_DefaultPermission_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PermissionPolicy.Name(default));
    }

    [Fact]
    public void TryParse_DeclaredPolicy_ReturnsPermission()
    {
        var parsed = PermissionPolicy.TryParse(
            "perm:operations:view",
            out var permission);

        Assert.True(parsed);
        Assert.Equal(TenantPermission.OperationsView, permission);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Admin")]
    [InlineData("perm:unknown")]
    public void TryParse_UndeclaredPolicy_ReturnsFalse(string? policyName)
    {
        var parsed = PermissionPolicy.TryParse(policyName, out var permission);

        Assert.False(parsed);
        Assert.Equal(default, permission);
    }

    [Fact]
    public void HasPermissionAttribute_DeclaredName_UsesTypedPolicy()
    {
        var attribute = new HasPermissionAttribute(TenantPermission.OperationsView);

        Assert.Equal(PermissionPolicy.Name(TenantPermission.OperationsView), attribute.Policy);
    }

    [Fact]
    public void HasPermissionAttribute_UndeclaredPermission_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new HasPermissionAttribute((TenantPermission)999));
    }
}
