using System.Globalization;
using Concertable.B2B.Concert.Domain.Events;
using Concertable.B2B.Concert.Infrastructure.Mappers;
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
        var venue = await BuildPartyAsync(e.VenueTenantId, e.VenueName, ct);
        var artist = await BuildPartyAsync(e.ArtistTenantId, e.ArtistName, ct);
        var when = e.Period.Start.ToString("dddd d MMMM yyyy", CultureInfo.InvariantCulture);

        var email = emailRenderer.Render(new BookingConfirmationEmailContent(venue, artist, when));

        await StageToMembersAsync(e.VenueTenantId, email, ct);
        await StageToMembersAsync(e.ArtistTenantId, email, ct);
    }

    private async Task<EmailParty> BuildPartyAsync(Guid tenantId, string displayName, CancellationToken ct)
    {
        if (!(await tenantModule.GetByIdAsync(tenantId, ct)).TryGetValue(out var tenant))
            throw new InvalidOperationException($"Tenant {tenantId} not found when sending its booking confirmation.");

        string? vat = null;
        string? address = null;
        if ((await tenantModule.GetTaxComplianceAsync(tenantId, ct)).TryGetValue(out var tax))
        {
            vat = string.IsNullOrWhiteSpace(tax.VatNumber) ? null : tax.VatNumber;
            var formatted = tax.RegisteredAddress.ToSingleLine();
            address = string.IsNullOrWhiteSpace(formatted) ? null : formatted;
        }

        return new EmailParty(displayName, tenant.LegalName, vat, address);
    }

    private async Task StageToMembersAsync(Guid tenantId, RenderedEmail email, CancellationToken ct)
    {
        var memberIds = await tenantModule.GetMemberUserIdsAsync(tenantId, ct);
        var emails = (await userModule.GetEmailsByIdsAsync(memberIds)).Values;
        foreach (var recipient in emails)
            await bus.SendAsync(new SendEmailCommand(recipient, email.Subject, email.HtmlBody), ct);
    }
}
