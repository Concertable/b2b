using Concertable.B2B.Tenant.Contracts;
using Concertable.B2B.Tenant.Domain;
using Concertable.B2B.Tenant.Domain.Events;
using Concertable.Kernel;

namespace Concertable.B2B.Tenant.UnitTests;

public sealed class TenantEntityTests
{
    [Fact]
    public void Create_ReturnsEntity_WithExpectedValues()
    {
        var userId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var tenant = TenantEntity.Create("Acme Ltd", userId, TenantType.Venue, now);

        Assert.NotEqual(Guid.Empty, tenant.Id);
        Assert.Equal("Acme Ltd", tenant.LegalName);
        Assert.Equal(TenantType.Venue, tenant.Type);
        Assert.Equal(userId, tenant.CreatedByUserId);
        Assert.Equal(now, tenant.CreatedAt);
    }

    [Fact]
    public void Create_PersistsThePersona()
    {
        var artistTenant = TenantEntity.Create("manager@acme.com", Guid.NewGuid(), TenantType.Artist, DateTime.UtcNow);

        Assert.Equal(TenantType.Artist, artistTenant.Type);
    }

    [Fact]
    public void Create_RaisesTenantCreatedDomainEvent_CarryingTheEmail()
    {
        var userId = Guid.NewGuid();

        var tenant = TenantEntity.Create("manager@acme.com", userId, TenantType.Venue, DateTime.UtcNow);

        var raised = Assert.IsType<TenantCreatedDomainEvent>(Assert.Single(tenant.DomainEvents));
        Assert.Equal(tenant.Id, raised.TenantId);
        Assert.Equal(userId, raised.CreatedByUserId);
        Assert.Equal("manager@acme.com", raised.Email);
    }

    [Fact]
    public void Announce_ReRaisesTenantCreatedDomainEvent_AfterEventsCleared()
    {
        var userId = Guid.NewGuid();
        var tenant = TenantEntity.Create("manager@acme.com", userId, TenantType.Artist, DateTime.UtcNow);
        tenant.ClearDomainEvents();

        tenant.Announce();

        var raised = Assert.IsType<TenantCreatedDomainEvent>(Assert.Single(tenant.DomainEvents));
        Assert.Equal(tenant.Id, raised.TenantId);
        Assert.Equal(userId, raised.CreatedByUserId);
        Assert.Equal("manager@acme.com", raised.Email);
    }

    [Fact]
    public void Create_LeavesTaxComplianceNull()
    {
        var tenant = TenantEntity.Create("Acme Ltd", Guid.NewGuid(), TenantType.Venue, DateTime.UtcNow);

        Assert.Null(tenant.TaxCompliance);
    }

    [Fact]
    public void UpdateLegalDetails_SetsLegalNameAndTaxCompliance()
    {
        var tenant = TenantEntity.Create("manager@acme.com", Guid.NewGuid(), TenantType.Venue, DateTime.UtcNow);
        var taxCompliance = new TaxCompliance(
            "GB123456789",
            "12345678",
            new RegisteredAddress("1 High Street", null, "Manchester", "M1 1AA", "United Kingdom"),
            "GB00BANK1234");

        tenant.UpdateLegalDetails("Acme Ltd", taxCompliance);

        Assert.Equal("Acme Ltd", tenant.LegalName);
        Assert.Equal(taxCompliance, tenant.TaxCompliance);
    }

    [Fact]
    public void UpdateLegalDetails_BlankLegalName_Throws()
    {
        var tenant = TenantEntity.Create("manager@acme.com", Guid.NewGuid(), TenantType.Venue, DateTime.UtcNow);
        var taxCompliance = new TaxCompliance(
            null,
            "12345678",
            new RegisteredAddress("1 High Street", null, "Manchester", "M1 1AA", "United Kingdom"),
            "GB00BANK1234");

        Assert.Throws<DomainException>(() => tenant.UpdateLegalDetails(" ", taxCompliance));
    }
}
