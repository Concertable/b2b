using Concertable.Shared.Email.Application;
using Microsoft.Extensions.Logging;

namespace Concertable.B2B.Tenant.Infrastructure.Services;

internal sealed class VerificationNotifier : IVerificationNotifier
{
    private readonly IEmailTransport emailTransport;
    private readonly ILogger<VerificationNotifier> logger;

    public VerificationNotifier(IEmailTransport emailTransport, ILogger<VerificationNotifier> logger)
    {
        this.emailTransport = emailTransport;
        this.logger = logger;
    }

    public Task NotifyApprovedAsync(TenantVerificationEntity verification, string? contactEmail) =>
        SendAsync(
            verification,
            contactEmail,
            "You're verified on Concertable",
            """
            Your organisation has been verified. You can now publish opportunities and receive payouts.
            """);

    public Task NotifyRejectedAsync(TenantVerificationEntity verification, string? contactEmail) =>
        SendAsync(
            verification,
            contactEmail,
            "Your Concertable verification needs attention",
            $"""
             Your verification submission was not approved.

             Reason: {verification.RejectionReason}

             You can submit new evidence at any time from your organisation settings.
             """);

    private async Task SendAsync(TenantVerificationEntity verification, string? contactEmail, string subject, string body)
    {
        if (contactEmail is null)
        {
            logger.VerificationContactEmailMissing(verification.TenantId);
            return;
        }

        await emailTransport.SendEmailAsync(contactEmail, subject, body);
    }
}
