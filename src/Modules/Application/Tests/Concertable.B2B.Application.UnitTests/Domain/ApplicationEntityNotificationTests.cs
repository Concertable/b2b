using Concertable.B2B.Application.Domain.Entities;
using Concertable.B2B.Application.Domain.Events;
using Concertable.B2B.Deal.Contracts.Enums;

namespace Concertable.B2B.Application.UnitTests;

public sealed class ApplicationEntityNotificationTests
{
    private static readonly Guid VenueTenantId = Guid.NewGuid();
    private static readonly Guid ArtistTenantId = Guid.NewGuid();

    [Theory]
    [InlineData(ApplicationNotification.Applied)]
    [InlineData(ApplicationNotification.Withdrawn)]
    public void NotifyCounterparty_AppliedOrWithdrawn_TargetsVenue(ApplicationNotification kind)
    {
        var application = CreateApplication();

        application.NotifyCounterparty(kind);

        var raised = Assert.IsType<ApplicationCounterpartyNotifiedDomainEvent>(Assert.Single(application.DomainEvents));
        Assert.Equal(kind, raised.Kind);
        Assert.Equal(VenueTenantId, raised.RecipientTenantId);
    }

    [Theory]
    [InlineData(ApplicationNotification.Accepted)]
    [InlineData(ApplicationNotification.Rejected)]
    [InlineData(ApplicationNotification.Cancelled)]
    public void NotifyCounterparty_ArtistFacingNotification_TargetsArtist(ApplicationNotification kind)
    {
        var application = CreateApplication();

        application.NotifyCounterparty(kind);

        var raised = Assert.IsType<ApplicationCounterpartyNotifiedDomainEvent>(Assert.Single(application.DomainEvents));
        Assert.Equal(kind, raised.Kind);
        Assert.Equal(ArtistTenantId, raised.RecipientTenantId);
    }

    private static ApplicationEntity CreateApplication() =>
        StandardApplication.Create(1, 1, DealType.FlatFee, VenueTenantId, ArtistTenantId);
}
