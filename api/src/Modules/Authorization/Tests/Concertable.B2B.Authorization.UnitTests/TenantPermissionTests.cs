using System.Reflection;

namespace Concertable.B2B.Authorization.UnitTests;

public sealed class TenantPermissionTests
{
    [Fact]
    public void TryParse_DeclaredNames_ReturnsMatchingInstances()
    {
        var nameFields = typeof(TenantPermission)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.IsLiteral
                && field.FieldType == typeof(string)
                && field.Name.EndsWith("Name", StringComparison.Ordinal))
            .ToList();

        Assert.Equal(TenantPermission.All.Count, nameFields.Count);

        foreach (var nameField in nameFields)
        {
            var value = Assert.IsType<string>(nameField.GetRawConstantValue());
            var instanceProperty = typeof(TenantPermission).GetProperty(
                nameField.Name[..^"Name".Length],
                BindingFlags.Public | BindingFlags.Static);

            Assert.NotNull(instanceProperty);
            var expected = Assert.IsType<TenantPermission>(instanceProperty.GetValue(null));
            Assert.True(TenantPermission.TryParse(value, out var parsed));
            Assert.Equal(expected, parsed);
            Assert.Equal(value, parsed.Value);
            Assert.Equal(value, parsed.ToString());
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("operations.View")]
    [InlineData("unknown")]
    public void TryParse_UndeclaredValue_ReturnsFalse(string? value)
    {
        var parsed = TenantPermission.TryParse(value, out var permission);

        Assert.False(parsed);
        Assert.Equal(default, permission);
    }
}
