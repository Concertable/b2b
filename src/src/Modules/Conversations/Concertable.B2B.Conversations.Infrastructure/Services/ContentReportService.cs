using Concertable.B2B.Conversations.Application.Errors;
using Concertable.B2B.Conversations.Application.Requests;
using Concertable.B2B.Tenant.Contracts;
using Microsoft.Extensions.Logging;
using Reunion;

namespace Concertable.B2B.Conversations.Infrastructure.Services;

internal sealed class ContentReportService : IContentReportService
{
    private readonly IMessageRepository messageRepository;
    private readonly IContentReportRepository reportRepository;
    private readonly IContentReportNotifier notifier;
    private readonly ICurrentUser currentUser;
    private readonly ITenantContext tenantContext;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<ContentReportService> logger;

    public ContentReportService(
        IMessageRepository messageRepository,
        IContentReportRepository reportRepository,
        IContentReportNotifier notifier,
        ICurrentUser currentUser,
        ITenantContext tenantContext,
        TimeProvider timeProvider,
        ILogger<ContentReportService> logger)
    {
        this.messageRepository = messageRepository;
        this.reportRepository = reportRepository;
        this.notifier = notifier;
        this.currentUser = currentUser;
        this.tenantContext = tenantContext;
        this.timeProvider = timeProvider;
        this.logger = logger;
    }

    public Task<UnitResult<ReportMessageError>> SubmitAsync(int messageId, ReportMessageRequest request) =>
        FindMessageAsync(messageId)
            .OrFailure<MessageEntity, ReportMessageError>(new ReportMessageError.MessageNotFound())
            .BindAsync(message => RecordAndNotifyAsync(message, request));

    private async Task<Option<MessageEntity>> FindMessageAsync(int messageId)
    {
        var message = await messageRepository.GetByIdAsync(messageId);

        // Your own tenant's message is not reportable. The inbox never offers the link, but the rule has
        // to hold server-side too — and "not yours" reads as absent, exactly like a foreign thread does.
        return message is null || message.SenderTenantId == tenantContext.GetTenantId() ? null : message;
    }

    private async Task<UnitResult<ReportMessageError>> RecordAndNotifyAsync(MessageEntity message, ReportMessageRequest request)
    {
        var report = ContentReportEntity.Create(
            message,
            tenantContext.GetTenantId(),
            currentUser.GetId(),
            request.Category,
            request.Details,
            timeProvider.GetUtcNow().DateTime);

        await reportRepository.AddAsync(report);
        await reportRepository.SaveChangesAsync();

        try
        {
            await notifier.SubmittedAsync(report);
        }
        catch (Exception exception)
        {
            // The persisted report is the record the duty turns on; a transport failure must not fail a
            // request whose write already committed, or the retry just files a duplicate.
            logger.ContentReportNotificationFailed(report.Reference, exception);
        }

        return new Success();
    }
}
