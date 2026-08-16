using Concertable.B2B.Conversations.Application.Errors;
using Concertable.B2B.Conversations.Application.Requests;
using Concertable.Contracts;
using Reunion;

namespace Concertable.B2B.Conversations.Infrastructure.Services;

internal sealed class ModerationService : IModerationService
{
    private readonly IAdminMessageRepository messageRepository;
    private readonly IAdminContentReportRepository reportRepository;
    private readonly ICurrentUser currentUser;
    private readonly TimeProvider timeProvider;

    public ModerationService(
        IAdminMessageRepository messageRepository,
        IAdminContentReportRepository reportRepository,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
    {
        this.messageRepository = messageRepository;
        this.reportRepository = reportRepository;
        this.currentUser = currentUser;
        this.timeProvider = timeProvider;
    }

    public async Task<IPagination<ContentReportDto>> GetQueueAsync(IPageParams pageParams) =>
        (await reportRepository.GetQueueAsync(pageParams)).Map(r => r.ToDto());

    public Task<UnitResult<ModerationError>> HideMessageAsync(int messageId) =>
        MutateMessageAsync(messageId, message => message.Hide(currentUser.GetId(), timeProvider.GetUtcNow().DateTime));

    public Task<UnitResult<ModerationError>> RestoreMessageAsync(int messageId) =>
        MutateMessageAsync(messageId, message => message.Restore(currentUser.GetId(), timeProvider.GetUtcNow().DateTime));

    public async Task<UnitResult<ModerationError>> ResolveReportAsync(int reportId, ResolveReportRequest request)
    {
        var report = await reportRepository.GetByIdAsync(reportId);
        if (report is null)
            return new ModerationError.ReportNotFound();

        if (report.Outcome is not null)
            return new ModerationError.AlreadyResolved();

        report.Resolve(request.Outcome, currentUser.GetId(), request.Notes, timeProvider.GetUtcNow().DateTime);
        await reportRepository.SaveChangesAsync();

        return new Success();
    }

    private async Task<UnitResult<ModerationError>> MutateMessageAsync(int messageId, Action<MessageEntity> mutate)
    {
        var message = await messageRepository.GetByIdAsync(messageId);
        if (message is null)
            return new ModerationError.MessageNotFound();

        mutate(message);
        await messageRepository.SaveChangesAsync();

        return new Success();
    }
}
