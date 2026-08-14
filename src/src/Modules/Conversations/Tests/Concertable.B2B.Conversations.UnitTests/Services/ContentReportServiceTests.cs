using Concertable.B2B.Conversations.Application.Errors;
using Concertable.B2B.Conversations.Application.Interfaces;
using Concertable.B2B.Conversations.Application.Requests;
using Concertable.B2B.Conversations.Application.Validators;
using Concertable.B2B.Conversations.Domain.Enums;
using Concertable.B2B.Conversations.Infrastructure.Services;
using Concertable.Kernel.Identity;
using Moq;

namespace Concertable.B2B.Conversations.UnitTests.Services;

public sealed class ContentReportServiceTests
{
    private static readonly Guid VenueTenantId = Guid.NewGuid();
    private static readonly Guid ArtistTenantId = Guid.NewGuid();
    private static readonly Guid ReportingUserId = Guid.NewGuid();

    private static MessageEntity Message() =>
        MessageEntity.Create(VenueTenantId, ArtistTenantId, senderTenantId: ArtistTenantId,
            sentByUserId: Guid.NewGuid(), "reported content", new DateTime(2026, 1, 1));

    private static ContentReportService Service(
        Mock<IMessageRepository> messages,
        Mock<IContentReportRepository> reports,
        Mock<IContentReportNotifier> notifier)
    {
        var currentUser = new Mock<ICurrentUser>();
        currentUser.SetupGet(u => u.Id).Returns(ReportingUserId);

        var tenantContext = new Mock<ITenantContext>();
        tenantContext.SetupGet(t => t.TenantId).Returns(VenueTenantId);

        return new ContentReportService(messages.Object, reports.Object, notifier.Object,
            currentUser.Object, tenantContext.Object, TimeProvider.System);
    }

    [Fact]
    public async Task Submit_PersistsTheReportAndNotifiesOnce()
    {
        var messages = new Mock<IMessageRepository>();
        messages.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(Message());

        ContentReportEntity? persisted = null;
        var reports = new Mock<IContentReportRepository>();
        reports.Setup(r => r.AddAsync(It.IsAny<ContentReportEntity>(), It.IsAny<CancellationToken>()))
            .Callback<ContentReportEntity, CancellationToken>((report, _) => persisted = report)
            .ReturnsAsync((ContentReportEntity report, CancellationToken _) => report);

        var notifier = new Mock<IContentReportNotifier>();

        var result = await Service(messages, reports, notifier)
            .SubmitAsync(7, new ReportMessageRequest { Category = ReportCategory.IllegalContent, Details = "why" });

        Assert.True(result.IsSuccess);
        Assert.NotNull(persisted);
        Assert.Equal(VenueTenantId, persisted.ReporterTenantId);
        Assert.Equal(ArtistTenantId, persisted.ReportedTenantId);
        Assert.Equal(ReportingUserId, persisted.ReportedByUserId);
        Assert.Equal(ReportCategory.IllegalContent, persisted.Category);
        reports.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        notifier.Verify(n => n.SubmittedAsync(persisted), Times.Once);
    }

    [Fact]
    public async Task Submit_MessageOutsideTheTenantsThreads_IsNotFound()
    {
        var messages = new Mock<IMessageRepository>();
        messages.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((MessageEntity?)null);

        var reports = new Mock<IContentReportRepository>();
        var notifier = new Mock<IContentReportNotifier>();

        var result = await Service(messages, reports, notifier)
            .SubmitAsync(404, new ReportMessageRequest { Category = ReportCategory.Spam });

        Assert.True(result.TryGetError(out var error));
        Assert.IsType<ReportMessageError.MessageNotFound>(error);
        reports.Verify(r => r.AddAsync(It.IsAny<ContentReportEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        notifier.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Submit_OverLongDetails_IsInvalidOnTheDetailsField()
    {
        var messages = new Mock<IMessageRepository>();
        messages.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(Message());

        var reports = new Mock<IContentReportRepository>();
        var notifier = new Mock<IContentReportNotifier>();

        var result = await Service(messages, reports, notifier).SubmitAsync(7, new ReportMessageRequest
        {
            Category = ReportCategory.Other,
            Details = new string('x', ContentReportValidators.MaxDetailsLength + 1)
        });

        Assert.True(result.TryGetError(out var error));
        var invalid = Assert.IsType<ReportMessageError.Invalid>(error);
        Assert.Contains(invalid.Errors, e => e.Key == "details");
        reports.Verify(r => r.AddAsync(It.IsAny<ContentReportEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        notifier.VerifyNoOtherCalls();
    }
}
