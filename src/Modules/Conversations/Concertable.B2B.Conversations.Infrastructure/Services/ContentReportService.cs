using Concertable.B2B.Conversations.Application.Errors;
using Concertable.B2B.Conversations.Application.Requests;
using Concertable.B2B.Conversations.Application.Validators;
using Concertable.B2B.Tenant.Contracts;
using FluentValidation;
using Reunion;
using Reunion.Validation;

namespace Concertable.B2B.Conversations.Infrastructure.Services;

internal sealed class ContentReportService : IContentReportService
{
    private readonly IMessageRepository messageRepository;
    private readonly IContentReportRepository reportRepository;
    private readonly IContentReportNotifier notifier;
    private readonly IValidator<ReportMessageRequest> validator;
    private readonly ICurrentUser currentUser;
    private readonly ITenantContext tenantContext;
    private readonly TimeProvider timeProvider;

    public ContentReportService(
        IMessageRepository messageRepository,
        IContentReportRepository reportRepository,
        IContentReportNotifier notifier,
        IValidator<ReportMessageRequest> validator,
        ICurrentUser currentUser,
        ITenantContext tenantContext,
        TimeProvider timeProvider)
    {
        this.messageRepository = messageRepository;
        this.reportRepository = reportRepository;
        this.notifier = notifier;
        this.validator = validator;
        this.currentUser = currentUser;
        this.tenantContext = tenantContext;
        this.timeProvider = timeProvider;
    }

    public Task<UnitResult<ReportMessageError>> SubmitAsync(int messageId, ReportMessageRequest request) =>
        FindMessageAsync(messageId)
            .OrFailure<MessageEntity, ReportMessageError>(new ReportMessageError.MessageNotFound())
            .BindAsync(message => ValidateAndRecordAsync(message, request));

    private async Task<Option<MessageEntity>> FindMessageAsync(int messageId) =>
        await messageRepository.GetByIdAsync(messageId);

    private async Task<UnitResult<ReportMessageError>> ValidateAndRecordAsync(MessageEntity message, ReportMessageRequest request)
    {
        if (validator.Validate(request).ToValidationResult().TryGetErrors(out var errors))
            return new ReportMessageError.Invalid(errors);

        var report = ContentReportEntity.Create(
            message,
            tenantContext.GetTenantId(),
            currentUser.GetId(),
            request.Category,
            request.Details,
            timeProvider.GetUtcNow().DateTime);

        await reportRepository.AddAsync(report);
        await reportRepository.SaveChangesAsync();

        await notifier.SubmittedAsync(report);

        return new Success();
    }
}
