using System.Text.Json;

namespace Concertable.B2B.Authorization.UnitTests;

public sealed class TenantPermissionTests
{
    [Fact]
    public void TryParseName_EveryDeclaredName_RoundTrips()
    {
        Assert.All(TenantPermission.All, permission =>
        {
            Assert.True(TenantPermission.TryParseName(permission.Name, out var parsed));
            Assert.Equal(permission, parsed);
        });
    }

    [Fact]
    public void Name_IsResourceColonAction()
    {
        Assert.Equal("concerts:ops_edit", TenantPermission.ConcertsOpsEdit.Name);
        Assert.Equal("tenant_settings:edit", TenantPermission.TenantSettingsEdit.Name);
        Assert.All(TenantPermission.All, permission => Assert.Matches("^[a-z_]+:[a-z_]+$", permission.Name));
    }

    [Fact]
    public void Json_UsesTheName()
    {
        Assert.Equal("\"messages:read\"", JsonSerializer.Serialize(TenantPermission.MessagesRead));
        Assert.Equal(TenantPermission.MessagesRead, JsonSerializer.Deserialize<TenantPermission>("\"messages:read\""));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("operations.view")]
    [InlineData("OperationsView")]
    [InlineData("unknown")]
    public void TryParseName_UndeclaredValue_ReturnsFalse(string? value)
    {
        var parsed = TenantPermission.TryParseName(value, out var permission);

        Assert.False(parsed);
        Assert.Equal(default, permission);
    }
}
