using Concertable.Shared.Email.Application;
using Microsoft.Extensions.Options;

namespace Concertable.B2B.Conversations.Infrastructure.Services;

internal sealed class ContentReportNotifier : IContentReportNotifier
{
    private readonly IEmailTransport emailTransport;
    private readonly ICurrentUser currentUser;
    private readonly SafetySettings settings;

    public ContentReportNotifier(
        IEmailTransport emailTransport,
        ICurrentUser currentUser,
        IOptions<SafetySettings> settings)
    {
        this.emailTransport = emailTransport;
        this.currentUser = currentUser;
        this.settings = settings.Value;
    }

    public async Task SubmittedAsync(ContentReportEntity report)
    {
        // Resolved before either send: Artifact 3 mandates the acknowledgement, so a reporter we cannot
        // reach is an invariant violation, not a mail we quietly drop.
        var reporterEmail = currentUser.Email
            ?? throw new UnauthorizedAccessException("The reporting user has no email address.");

        var reference = report.Reference;

        await emailTransport.SendEmailAsync(
            settings.ReportInboxEmail,
            $"Content report {reference}",
            $"""
             Reference: {reference}
             Category: {report.Category}
             Submitted: {report.SubmittedAt:u}
             Message id: {report.MessageId}
             Reporting tenant: {report.ReporterTenantId}
             Reported tenant: {report.ReportedTenantId}
             Reported by user: {report.ReportedByUserId}

             Details:
             {report.Details ?? "(none provided)"}

             Message excerpt:
             {report.MessageExcerpt}
             """);

        await emailTransport.SendEmailAsync(
            reporterEmail,
            $"We have received your report ({reference})",
            $"""
             Thank you for reporting this message.

             Your reference is {reference}. Our team will review the report and take any action needed.
             Quote this reference in any follow-up about the report or its outcome.
             """);
    }
}
