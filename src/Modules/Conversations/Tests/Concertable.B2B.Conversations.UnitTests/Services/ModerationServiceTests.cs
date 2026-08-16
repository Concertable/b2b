using Concertable.B2B.Conversations.Application.Errors;
using Concertable.B2B.Conversations.Application.Interfaces;
using Concertable.B2B.Conversations.Application.Requests;
using Concertable.B2B.Conversations.Domain.Enums;
using Concertable.B2B.Conversations.Infrastructure.Services;
using Concertable.Kernel.Identity;
using Moq;

namespace Concertable.B2B.Conversations.UnitTests.Services;

public sealed class ModerationServiceTests
{
    private static readonly Guid AdminUserId = Guid.NewGuid();

    private static MessageEntity Message() =>
        MessageEntity.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "content", new DateTime(2026, 1, 1));

    private static ContentReportEntity Report() =>
        ContentReportEntity.Create(Message(), Guid.NewGuid(), Guid.NewGuid(),
            ReportCategory.IllegalContent, null, new DateTime(2026, 8, 14));

    private static ModerationService Service(
        Mock<IAdminMessageRepository> messages,
        Mock<IAdminContentReportRepository> reports)
    {
        var currentUser = new Mock<ICurrentUser>();
        currentUser.SetupGet(u => u.Id).Returns(AdminUserId);

        return new ModerationService(messages.Object, reports.Object, currentUser.Object, TimeProvider.System);
    }

    [Fact]
    public async Task Hide_UnknownMessage_IsNotFound_AndSavesNothing()
    {
        var messages = new Mock<IAdminMessageRepository>();
        messages.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MessageEntity?)null);
        var reports = new Mock<IAdminContentReportRepository>();

        var result = await Service(messages, reports).HideMessageAsync(404);

        Assert.True(result.TryGetError(out var error));
        Assert.IsType<ModerationError.MessageNotFound>(error);
        messages.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Restore_UnknownMessage_IsNotFound_AndSavesNothing()
    {
        var messages = new Mock<IAdminMessageRepository>();
        messages.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MessageEntity?)null);
        var reports = new Mock<IAdminContentReportRepository>();

        var result = await Service(messages, reports).RestoreMessageAsync(404);

        Assert.True(result.TryGetError(out var error));
        Assert.IsType<ModerationError.MessageNotFound>(error);
        messages.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Restore_StampsTheActingAdmin_WithoutErasingTheHide()
    {
        var message = Message();
        message.Hide(Guid.NewGuid(), new DateTime(2026, 8, 15));
        var messages = new Mock<IAdminMessageRepository>();
        messages.Setup(r => r.GetByIdAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync(message);
        var reports = new Mock<IAdminContentReportRepository>();

        var result = await Service(messages, reports).RestoreMessageAsync(7);

        Assert.True(result.IsSuccess);
        Assert.False(message.IsHidden);
        Assert.NotNull(message.HiddenAt);
        Assert.Equal(AdminUserId, message.RestoredByUserId);
    }

    [Fact]
    public async Task Hide_StampsTheActingAdmin_AndSavesOnce()
    {
        var message = Message();
        var messages = new Mock<IAdminMessageRepository>();
        messages.Setup(r => r.GetByIdAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync(message);
        var reports = new Mock<IAdminContentReportRepository>();

        var result = await Service(messages, reports).HideMessageAsync(7);

        Assert.True(result.IsSuccess);
        Assert.NotNull(message.HiddenAt);
        Assert.Equal(AdminUserId, message.HiddenByUserId);
        messages.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Resolve_UnknownReport_IsNotFound_AndSavesNothing()
    {
        var messages = new Mock<IAdminMessageRepository>();
        var reports = new Mock<IAdminContentReportRepository>();
        reports.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ContentReportEntity?)null);

        var result = await Service(messages, reports)
            .ResolveReportAsync(404, new ResolveReportRequest { Outcome = ReportOutcome.NoActionTaken });

        Assert.True(result.TryGetError(out var error));
        Assert.IsType<ModerationError.ReportNotFound>(error);
        reports.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Resolve_AlreadyResolvedReport_Conflicts_AndSavesNothing()
    {
        var report = Report();
        report.Resolve(ReportOutcome.ContentRemoved, Guid.NewGuid(), null, new DateTime(2026, 8, 15));

        var messages = new Mock<IAdminMessageRepository>();
        var reports = new Mock<IAdminContentReportRepository>();
        reports.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(report);

        var result = await Service(messages, reports)
            .ResolveReportAsync(1, new ResolveReportRequest { Outcome = ReportOutcome.NoActionTaken });

        Assert.True(result.TryGetError(out var error));
        Assert.IsType<ModerationError.AlreadyResolved>(error);
        reports.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
