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

    public Task<UnitResult<ReportMessageError>> SubmitAsync(
        int conversationId,
        int messageId,
        ReportMessageRequest request) =>
        FindMessageAsync(conversationId, messageId)
            .OrFailure<MessageEntity, ReportMessageError>(new ReportMessageError.MessageNotFound())
            .BindAsync(message => RecordAndNotifyAsync(message, request));

    private async Task<Option<MessageEntity>> FindMessageAsync(int conversationId, int messageId)
    {
        var message = await messageRepository.GetByIdAsync(messageId);

        return message is null
            || message.ConversationId != conversationId
            || message.SenderTenantId == tenantContext.GetTenantId()
                ? null
                : message;
    }

    private async Task<UnitResult<ReportMessageError>> RecordAndNotifyAsync(MessageEntity message, ReportMessageRequest request)
    {
        var report = ContentReportEntity.Create(
            message,
            tenantContext.GetTenantId(),
            currentUser.GetId(),
            request.Category,
            request.Details,
            timeProvider.GetUtcNow().UtcDateTime);

        await reportRepository.AddAsync(report);
        await reportRepository.SaveChangesAsync();

        try
        {
            await notifier.SubmittedAsync(report);
        }
        catch (Exception exception)
        {
            logger.ContentReportNotificationFailed(report.Reference, exception);
        }

        return new Success();
    }
}
