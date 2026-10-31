using System.Text.Json;

namespace Concertable.B2B.Authorization.UnitTests;

public sealed class TenantPermissionTests
{
    [Fact]
    public void TryParseKey_EveryDeclaredKey_RoundTrips()
    {
        Assert.All(TenantPermission.All, permission =>
        {
            Assert.True(TenantPermission.TryParseKey(permission.Key, out var parsed));
            Assert.Equal(permission, parsed);
        });
    }

    [Fact]
    public void Key_IsResourceColonAction()
    {
        Assert.Equal("concerts:ops_edit", TenantPermission.ConcertsOpsEdit.Key);
        Assert.Equal("tenant_settings:edit", TenantPermission.TenantSettingsEdit.Key);
        Assert.All(TenantPermission.All, permission => Assert.Matches("^[a-z_]+:[a-z_]+$", permission.Key));
    }

    [Fact]
    public void Json_UsesTheKey()
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
    public void TryParseKey_UndeclaredValue_ReturnsFalse(string? value)
    {
        var parsed = TenantPermission.TryParseKey(value, out var permission);

        Assert.False(parsed);
        Assert.Equal(default, permission);
    }
}
