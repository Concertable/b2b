using Concertable.Shared.Email.Application;

namespace Concertable.B2B.Tenant.Infrastructure.Services;

internal sealed class VerificationNotifier : IVerificationNotifier
{
    private readonly IEmailTransport emailTransport;

    public VerificationNotifier(IEmailTransport emailTransport)
    {
        this.emailTransport = emailTransport;
    }

    public Task NotifyApprovedAsync(TenantVerificationEntity verification, string contactEmail) =>
        emailTransport.SendEmailAsync(
            contactEmail,
            "You're verified on Concertable",
            """
            Your organisation has been verified. You can now publish opportunities and receive payouts.
            """);

    public Task NotifyRejectedAsync(TenantVerificationEntity verification, string contactEmail) =>
        emailTransport.SendEmailAsync(
            contactEmail,
            "Your Concertable verification needs attention",
            $"""
             Your verification submission was not approved.

             Reason: {verification.RejectionReason}

             You can submit new evidence at any time from your organisation settings.
             """);
}
