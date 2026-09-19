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

        var tenant = TenantEntity.Create("Acme Ltd", "contact@acme.test", userId, now);

        Assert.NotEqual(Guid.Empty, tenant.Id);
        Assert.Equal("Acme Ltd", tenant.LegalName);
        Assert.Equal("Acme Ltd", tenant.DisplayName);
        Assert.Equal(1, tenant.DisplayVersion);
        Assert.Equal("contact@acme.test", tenant.ContactEmail);
        Assert.Equal(userId, tenant.CreatedByUserId);
        Assert.Equal(now, tenant.CreatedAt);
        Assert.Equal(1, tenant.Version);
        Assert.Equal(1, tenant.EligibilityVersion);
    }

    [Fact]
    public void Create_ActivatesNoBusinessActivity()
    {
        var tenant = TenantEntity.Create("Acme Ltd", "manager@acme.com", Guid.NewGuid(), DateTime.UtcNow);

        Assert.Empty(tenant.BusinessActivities);
    }

    [Fact]
    public void Create_RaisesTenantCreatedDomainEvent_CarryingTheEmail()
    {
        var userId = Guid.NewGuid();

        var tenant = TenantEntity.Create(
            "Acme Ltd",
            "manager@acme.com",
            userId,
            DateTime.UtcNow);

        var raised = Assert.Single(tenant.DomainEvents.OfType<TenantCreatedDomainEvent>());
        Assert.Equal(tenant.Id, raised.TenantId);
        Assert.Equal(userId, raised.CreatedByUserId);
        Assert.Equal("manager@acme.com", raised.Email);

        var displayChanged = Assert.Single(tenant.DomainEvents.OfType<TenantDisplayChangedDomainEvent>());
        Assert.Same(tenant, displayChanged.Tenant);
    }

    [Fact]
    public void Announce_ReRaisesTenantCreatedDomainEvent_AfterEventsCleared()
    {
        var userId = Guid.NewGuid();
        var tenant = TenantEntity.Create(
            "Acme Ltd",
            "manager@acme.com",
            userId,
            DateTime.UtcNow);
        tenant.ClearDomainEvents();

        tenant.Announce();

        var raised = Assert.Single(tenant.DomainEvents.OfType<TenantCreatedDomainEvent>());
        Assert.Equal(tenant.Id, raised.TenantId);
        Assert.Equal(userId, raised.CreatedByUserId);
        Assert.Equal("manager@acme.com", raised.Email);

        var displayChanged = Assert.Single(tenant.DomainEvents.OfType<TenantDisplayChangedDomainEvent>());
        Assert.Same(tenant, displayChanged.Tenant);
    }

    [Fact]
    public void Create_LeavesTaxComplianceNull()
    {
        var tenant = TenantEntity.Create(
            "Acme Ltd",
            "manager@acme.com",
            Guid.NewGuid(),
            DateTime.UtcNow);

        Assert.Null(tenant.TaxCompliance);
    }

    [Fact]
    public void UpdateLegalDetails_ValidFields_UpdatesTheTenant()
    {
        var tenant = TenantEntity.Create(
            "Manager business",
            "manager@acme.com",
            Guid.NewGuid(),
            DateTime.UtcNow);
        var taxCompliance = TaxComplianceValue();
        tenant.ClearDomainEvents();

        var result = tenant.UpdateLegalDetails("Acme Ltd", taxCompliance);

        Assert.True(result.IsSuccess);
        Assert.Equal("Acme Ltd", tenant.LegalName);
        Assert.Equal(taxCompliance, tenant.TaxCompliance);
        Assert.Equal("Acme Ltd", tenant.EffectiveDisplayName);
        Assert.Equal(2, tenant.DisplayVersion);
        Assert.Equal(2, tenant.Version);
        Assert.Equal(1, tenant.EligibilityVersion);
        Assert.Single(tenant.DomainEvents.OfType<TenantDisplayChangedDomainEvent>());
    }

    [Fact]
    public void UpdateLegalDetails_InvalidFields_ReturnsStructuredErrorsWithoutMutation()
    {
        var tenant = TenantEntity.Create(
            "Manager business",
            "manager@acme.com",
            Guid.NewGuid(),
            DateTime.UtcNow);

        var result = tenant.UpdateLegalDetails(" ", null!);

        Assert.True(result.TryGetError(out var errors));
        Assert.Equal(["LegalName is required."], errors.Errors["LegalName"]);
        Assert.Equal(["TaxCompliance is required."], errors.Errors["TaxCompliance"]);
        Assert.Equal("Manager business", tenant.LegalName);
        Assert.Null(tenant.TaxCompliance);
    }

    [Fact]
    public void UpdateContactEmail_ChangedEmail_IncrementsVersionWithoutChangingEligibility()
    {
        var tenant = TenantEntity.Create(
            "Acme Ltd",
            "old@acme.test",
            Guid.NewGuid(),
            DateTime.UtcNow);

        var result = tenant.UpdateContactEmail("new@acme.test");

        Assert.True(result.IsSuccess);
        Assert.Equal("new@acme.test", tenant.ContactEmail);
        Assert.Equal(2, tenant.Version);
        Assert.Equal(1, tenant.EligibilityVersion);
    }

    [Fact]
    public void ActivateBusinessActivity_ChangedEligibility_IncrementsBothVersions()
    {
        var tenant = TenantEntity.Create(
            "Acme Ltd",
            "contact@acme.test",
            Guid.NewGuid(),
            DateTime.UtcNow);

        tenant.ActivateBusinessActivity(TenantBusinessActivityKind.VenueOperator, DateTime.UtcNow);

        Assert.True(tenant.HasActiveActivity(TenantBusinessActivityKind.VenueOperator));
        Assert.Equal(2, tenant.Version);
        Assert.Equal(2, tenant.EligibilityVersion);
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
