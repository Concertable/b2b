using Concertable.B2B.Tenant.Domain;
using Concertable.B2B.Tenant.Domain.Events;

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

    [Fact]
    public void Create_RaisesTenantCreatedDomainEvent()
    {
        var userId = Guid.NewGuid();

        var tenant = TenantEntity.Create("Acme Ltd", userId, DateTime.UtcNow);

        var raised = Assert.IsType<TenantCreatedDomainEvent>(Assert.Single(tenant.DomainEvents));
        Assert.Equal(tenant.Id, raised.TenantId);
        Assert.Equal(userId, raised.CreatedByUserId);
    }

    [Fact]
    public void Announce_RaisesTenantCreatedDomainEvent_ForExistingTenant()
    {
        var userId = Guid.NewGuid();
        var tenant = TenantEntity.Create("Acme Ltd", userId, DateTime.UtcNow);
        tenant.ClearDomainEvents();

        tenant.Announce();

        var raised = Assert.IsType<TenantCreatedDomainEvent>(Assert.Single(tenant.DomainEvents));
        Assert.Equal(tenant.Id, raised.TenantId);
        Assert.Equal(userId, raised.CreatedByUserId);
    }
}

