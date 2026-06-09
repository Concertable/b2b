using Concertable.B2B.Tenant.Domain;

namespace Concertable.B2B.Tenant.UnitTests;

public sealed class TenantEntityTests
{
    [Fact]
    public void Create_ReturnsEntity_WithExpectedValues()
    {
        var userId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var tenant = TenantEntity.Create("Acme Ltd", userId, now);

        Assert.NotEqual(Guid.Empty, tenant.Id);
        Assert.Equal("Acme Ltd", tenant.LegalName);
        Assert.Equal(userId, tenant.CreatedByUserId);
        Assert.Equal(now, tenant.CreatedAt);
    }
}

