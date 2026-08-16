using Concertable.B2B.Tenant.Contracts;
using Concertable.B2B.Tenant.Domain.Entities;
using Concertable.B2B.Tenant.Domain.Events;
using Concertable.B2B.Tenant.Domain.ValueObjects;

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
        var artistTenant = TenantEntity.Create(
            "manager@acme.com",
            Guid.NewGuid(),
            TenantType.Artist,
            DateTime.UtcNow);

        Assert.Equal(TenantType.Artist, artistTenant.Type);
    }

    [Fact]
    public void Create_RaisesTenantCreatedDomainEvent_CarryingTheEmail()
    {
        var userId = Guid.NewGuid();

        var tenant = TenantEntity.Create(
            "manager@acme.com",
            userId,
            TenantType.Venue,
            DateTime.UtcNow);

        var raised = Assert.IsType<TenantCreatedDomainEvent>(Assert.Single(tenant.DomainEvents));
        Assert.Equal(tenant.Id, raised.TenantId);
        Assert.Equal(userId, raised.CreatedByUserId);
        Assert.Equal("manager@acme.com", raised.Email);
    }

    [Fact]
    public void Announce_ReRaisesTenantCreatedDomainEvent_AfterEventsCleared()
    {
        var userId = Guid.NewGuid();
        var tenant = TenantEntity.Create(
            "manager@acme.com",
            userId,
            TenantType.Artist,
            DateTime.UtcNow);
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
        var tenant = TenantEntity.Create(
            "Acme Ltd",
            Guid.NewGuid(),
            TenantType.Venue,
            DateTime.UtcNow);

        Assert.Null(tenant.TaxCompliance);
    }

    [Fact]
    public void UpdateLegalDetails_ValidFields_UpdatesTheTenant()
    {
        var tenant = TenantEntity.Create(
            "manager@acme.com",
            Guid.NewGuid(),
            TenantType.Venue,
            DateTime.UtcNow);
        var taxCompliance = TaxComplianceValue();

        var result = tenant.UpdateLegalDetails("Acme Ltd", taxCompliance);

        Assert.True(result.IsSuccess);
        Assert.Equal("Acme Ltd", tenant.LegalName);
        Assert.Equal(taxCompliance, tenant.TaxCompliance);
    }

    [Fact]
    public void UpdateLegalDetails_InvalidFields_ReturnsStructuredErrorsWithoutMutation()
    {
        var tenant = TenantEntity.Create(
            "manager@acme.com",
            Guid.NewGuid(),
            TenantType.Venue,
            DateTime.UtcNow);

        var result = tenant.UpdateLegalDetails(" ", null!);

        Assert.True(result.TryGetError(out var errors));
        Assert.Equal(["LegalName is required."], errors.Errors["LegalName"]);
        Assert.Equal(["TaxCompliance is required."], errors.Errors["TaxCompliance"]);
        Assert.Equal("manager@acme.com", tenant.LegalName);
        Assert.Null(tenant.TaxCompliance);
    }

    private static TaxCompliance TaxComplianceValue() => RegisteredAddress
        .Create("1 High Street", null, "Manchester", "M1 1AA", "United Kingdom")
        .Bind(address => TaxCompliance.Create(
            "GB123456789",
            "12345678",
            address,
            "GB00BANK1234",
            true))
        .Match(
            compliance => compliance,
            _ => throw new InvalidOperationException("Test tax compliance is invalid."));
}
