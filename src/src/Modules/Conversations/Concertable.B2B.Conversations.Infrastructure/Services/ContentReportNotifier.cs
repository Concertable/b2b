using Concertable.Shared.Email.Application;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Concertable.B2B.Conversations.Infrastructure.Services;

internal sealed class ContentReportNotifier : IContentReportNotifier
{
    private readonly IEmailTransport emailTransport;
    private readonly ICurrentUser currentUser;
    private readonly SafetySettings settings;
    private readonly ILogger<ContentReportNotifier> logger;

    public ContentReportNotifier(
        IEmailTransport emailTransport,
        ICurrentUser currentUser,
        IOptions<SafetySettings> settings,
        ILogger<ContentReportNotifier> logger)
    {
        this.emailTransport = emailTransport;
        this.currentUser = currentUser;
        this.settings = settings.Value;
        this.logger = logger;
    }

    public async Task SubmittedAsync(ContentReportEntity report)
    {
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

        var reporterEmail = currentUser.Email;
        if (reporterEmail is null)
        {
            logger.ReporterEmailMissing(reference);
            return;
        }

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
