using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Tenant.Contracts;
using Concertable.B2B.User.Contracts;
using Concertable.Kernel.ValueObjects;
using Concertable.Shared.Email.Application;

namespace Concertable.B2B.Concert.Infrastructure.Services;

internal sealed class BookingConfirmationNotifier : IBookingConfirmationNotifier
{
    private readonly ITenantModule tenantModule;
    private readonly IUserModule userModule;
    private readonly IBookingConfirmationEmailGenerator generator;
    private readonly IEmailTransport emailTransport;

    public BookingConfirmationNotifier(
        ITenantModule tenantModule,
        IUserModule userModule,
        IBookingConfirmationEmailGenerator generator,
        IEmailTransport emailTransport)
    {
        this.tenantModule = tenantModule;
        this.userModule = userModule;
        this.generator = generator;
        this.emailTransport = emailTransport;
    }

    public async Task BookingConfirmedAsync(Guid venueTenantId, string venueName, Guid artistTenantId, string artistName, DateRange period)
    {
        var venue = await BuildPartyAsync(venueTenantId, venueName);
        var artist = await BuildPartyAsync(artistTenantId, artistName);

        var email = generator.Generate(venue, artist, period);

        await SendToMembersAsync(venueTenantId, email);
        await SendToMembersAsync(artistTenantId, email);
    }

    private async Task<BookingConfirmationParty> BuildPartyAsync(Guid tenantId, string displayName)
    {
        var tenant = await tenantModule.GetByIdAsync(tenantId)
            ?? throw new InvalidOperationException($"Tenant {tenantId} not found when sending its booking confirmation.");
        var tax = await tenantModule.GetTaxComplianceAsync(tenantId);
        return new BookingConfirmationParty(displayName, tenant, tax);
    }

    private async Task SendToMembersAsync(Guid tenantId, BookingConfirmationEmail email)
    {
        var memberIds = await tenantModule.GetMemberUserIdsAsync(tenantId);
        var emails = (await userModule.GetEmailsByIdsAsync(memberIds)).Values;
        foreach (var recipient in emails)
            await emailTransport.SendEmailAsync(recipient, email.Subject, email.Body);
    }
}
