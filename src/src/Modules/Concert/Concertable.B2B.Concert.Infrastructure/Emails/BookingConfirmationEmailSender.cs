using System.Globalization;
using Concertable.B2B.Concert.Domain.Events;
using Concertable.B2B.Tenant.Contracts;
using Concertable.B2B.User.Contracts;
using Concertable.Messaging.Contracts;
using Concertable.Shared.Email.Application;

namespace Concertable.B2B.Concert.Infrastructure.Emails;

/// <summary>
/// Builds the booking-confirmation email and stages one <see cref="SendEmailCommand"/> per member of both
/// tenants on the outbox. Resolves each party's legal trading details (VAT/address absent until tax-compliance
/// setup) and renders the shared MJML template. Called by the pre-commit domain-event handler, so the sends
/// commit with the booking and are retried by the outbox.
/// </summary>
internal sealed class BookingConfirmationEmailSender
{
    private readonly ITenantModule tenantModule;
    private readonly IUserModule userModule;
    private readonly IEmailRenderer emailRenderer;
    private readonly IBus bus;

    public BookingConfirmationEmailSender(
        ITenantModule tenantModule,
        IUserModule userModule,
        IEmailRenderer emailRenderer,
        IBus bus)
    {
        this.tenantModule = tenantModule;
        this.userModule = userModule;
        this.emailRenderer = emailRenderer;
        this.bus = bus;
    }

    public async Task SendAsync(BookingConfirmedDomainEvent e, CancellationToken ct = default)
    {
        var venue = await BuildPartyAsync(e.VenueTenantId, e.VenueName);
        var artist = await BuildPartyAsync(e.ArtistTenantId, e.ArtistName);
        var when = e.Period.Start.ToString("dddd d MMMM yyyy", CultureInfo.InvariantCulture);

        var email = emailRenderer.Render(new BookingConfirmationEmailContent(venue, artist, when));

        await StageToMembersAsync(e.VenueTenantId, email, ct);
        await StageToMembersAsync(e.ArtistTenantId, email, ct);
    }

    private async Task<EmailParty> BuildPartyAsync(Guid tenantId, string displayName)
    {
        var tenant = await tenantModule.GetByIdAsync(tenantId)
            ?? throw new InvalidOperationException($"Tenant {tenantId} not found when sending its booking confirmation.");
        var tax = await tenantModule.GetTaxComplianceAsync(tenantId);
        var vat = string.IsNullOrWhiteSpace(tax?.VatNumber) ? null : tax!.VatNumber;
        var address = tax is null ? null : FormatAddress(tax.RegisteredAddress);
        return new EmailParty(displayName, tenant.LegalName, vat, string.IsNullOrWhiteSpace(address) ? null : address);
    }

    private static string FormatAddress(RegisteredAddressDto address) =>
        string.Join(", ", new[]
        {
            address.Line1,
            address.Line2,
            address.City,
            address.Postcode,
            address.Country
        }.Where(part => !string.IsNullOrWhiteSpace(part)));

    private async Task StageToMembersAsync(Guid tenantId, RenderedEmail email, CancellationToken ct)
    {
        var memberIds = await tenantModule.GetMemberUserIdsAsync(tenantId);
        var emails = (await userModule.GetEmailsByIdsAsync(memberIds)).Values;
        foreach (var recipient in emails)
            await bus.SendAsync(new SendEmailCommand(recipient, email.Subject, email.HtmlBody), ct);
    }
}
