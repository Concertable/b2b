using Concertable.B2B.Concert.Domain.Events;
using Concertable.B2B.Concert.Infrastructure.Emails;
using Concertable.B2B.Tenant.Contracts;
using Concertable.B2B.User.Contracts;
using Concertable.Kernel.ValueObjects;
using Reunion;
using Concertable.Messaging.Contracts;
using Concertable.Shared.Email.Application;
using Moq;
using Xunit;

namespace Concertable.B2B.Concert.UnitTests.Emails;

public sealed class BookingConfirmationEmailSenderTests
{
    private static readonly Guid VenueTenant = Guid.NewGuid();
    private static readonly Guid ArtistTenant = Guid.NewGuid();

    [Fact]
    public async Task Stages_OneEmailPerMemberOfBothTenants_ResolvingLegalDetailsPerTaxCompliance()
    {
        var venueMember1 = Guid.NewGuid();
        var venueMember2 = Guid.NewGuid();
        var artistMember = Guid.NewGuid();

        var tenant = new Mock<ITenantModule>();
        tenant.Setup(m => m.GetByIdAsync(VenueTenant, It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(Option.Some(new TenantDto(VenueTenant, "Venue Legal Ltd"))));
        tenant.Setup(m => m.GetByIdAsync(ArtistTenant, It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(Option.Some(new TenantDto(ArtistTenant, "Artist Legal Ltd"))));
        tenant.Setup(m => m.GetTaxComplianceAsync(VenueTenant, It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(Option.Some(new TaxComplianceDto
            {
                VatNumber = "GB123456789",
                SellerIdentifier = "SELLER-1",
                RegisteredAddress = new RegisteredAddressDto
                {
                    Line1 = "1 Main St",
                    City = "London",
                    Postcode = "EC1A 1AA",
                    Country = "United Kingdom"
                },
                BankReference = "BANK-1",
                HoldsMusicLicence = true
            })));
        tenant.Setup(m => m.GetTaxComplianceAsync(ArtistTenant, It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(Option.None<TaxComplianceDto>()));
        tenant.Setup(m => m.GetMemberUserIdsAsync(VenueTenant, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Guid> { venueMember1, venueMember2 });
        tenant.Setup(m => m.GetMemberUserIdsAsync(ArtistTenant, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Guid> { artistMember });

        var addresses = new Dictionary<Guid, string>
        {
            [venueMember1] = "v1@example.com",
            [venueMember2] = "v2@example.com",
            [artistMember] = "a1@example.com"
        };
        var user = new Mock<IUserModule>();
        user.Setup(m => m.GetEmailsByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync((IEnumerable<Guid> ids) => ids.ToDictionary(id => id, id => addresses[id]));

        BookingConfirmationEmailContent? captured = null;
        var renderer = new Mock<IEmailRenderer>();
        renderer.Setup(r => r.Render(It.IsAny<IEmailContent>()))
            .Returns((IEmailContent content) =>
            {
                captured = Assert.IsType<BookingConfirmationEmailContent>(content);
                return new RenderedEmail(content.Subject, "<html>rendered</html>");
            });

        var staged = new List<SendEmailCommand>();
        var bus = new Mock<IBus>();
        bus.Setup(b => b.SendAsync(It.IsAny<SendEmailCommand>(), It.IsAny<CancellationToken>()))
            .Callback((SendEmailCommand cmd, CancellationToken _) => staged.Add(cmd))
            .Returns(Task.CompletedTask);

        var sender = new BookingConfirmationEmailSender(tenant.Object, user.Object, renderer.Object, bus.Object);

        var period = new DateRange(
            new DateTime(2035, 1, 1, 19, 0, 0, DateTimeKind.Utc),
            new DateTime(2035, 1, 1, 22, 0, 0, DateTimeKind.Utc));
        await sender.SendAsync(new BookingConfirmedDomainEvent(VenueTenant, "The Venue", ArtistTenant, "The Artist", period));

        // One staged send per member across both tenants.
        Assert.Equal(3, staged.Count);
        Assert.Equal(
            new[] { "a1@example.com", "v1@example.com", "v2@example.com" },
            staged.Select(c => c.To).OrderBy(to => to).ToArray());
        Assert.All(staged, c => Assert.Equal("Booking confirmed: The Artist at The Venue", c.Subject));
        Assert.All(staged, c => Assert.Equal("<html>rendered</html>", c.Body));

        // Legal details resolved per tax compliance: venue fully populated, artist name-only.
        Assert.NotNull(captured);
        Assert.Equal("Venue Legal Ltd", captured!.Venue.LegalName);
        Assert.Equal("GB123456789", captured.Venue.Vat);
        Assert.Equal("1 Main St, London, EC1A 1AA, United Kingdom", captured.Venue.Address);
        Assert.Equal("Artist Legal Ltd", captured.Artist.LegalName);
        Assert.Null(captured.Artist.Vat);
        Assert.Null(captured.Artist.Address);
    }
}
