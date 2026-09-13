using Concertable.B2B.Conversations.Application.DTOs;
using Concertable.B2B.Conversations.Application.Interfaces;
using Concertable.B2B.Conversations.Infrastructure;
using Concertable.B2B.Conversations.Domain.ReadModels;
using Concertable.B2B.Conversations.Infrastructure.Services;
using Concertable.B2B.Tenant.Contracts;
using Concertable.B2B.Tenant.Contracts.Events;
using Concertable.B2B.User.Contracts;
using Concertable.Contracts;
using Concertable.Kernel.Identity;
using Concertable.Messaging.Contracts;
using Reunion;
using Moq;

namespace Concertable.B2B.Conversations.UnitTests.Services;

public sealed class MessageServiceTests
{
    private readonly Mock<IMessageRepository> repository;
    private readonly Mock<IConversationsNotifier> notifier;
    private readonly Mock<IBus> bus;
    private readonly Mock<ITenantContext> tenantContext;
    private readonly Mock<ITenantModule> tenantModule;
    private readonly MessageService sut;

    public MessageServiceTests()
    {
        this.repository = new Mock<IMessageRepository>();
        this.notifier = new Mock<IConversationsNotifier>();
        this.bus = new Mock<IBus>();
        this.bus.Setup(value => value.PublishAsync(
                It.IsAny<TenantActivityRecordedEvent>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        this.tenantContext = new Mock<ITenantContext>();
        this.tenantModule = new Mock<ITenantModule>();
        this.sut = new MessageService(
            this.repository.Object,
            this.notifier.Object,
            this.bus.Object,
            new InlineOutboxBehavior(),
            Mock.Of<ICurrentUser>(),
            this.tenantContext.Object,
            this.tenantModule.Object,
            Mock.Of<IUserModule>(),
            TimeProvider.System);
    }

    [Fact]
    public async Task GetRecentPreviews_ResolvesCounterpartyIdentityAndSurfaceHref()
    {
        var activeTenantId = Guid.NewGuid();
        var venueTenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var at = new DateTime(2026, 8, 14, 12, 0, 0, DateTimeKind.Utc);
        var repository = new Mock<IMessageRepository>();
        repository.Setup(r => r.GetRecentPreviewsAsync(activeTenantId, userId))
            .ReturnsAsync([new(12, venueTenantId, true, "See you Friday", at, true)]);
        var currentUser = new Mock<ICurrentUser>();
        currentUser.SetupGet(u => u.Id).Returns(userId);
        var tenantContext = new Mock<ITenantContext>();
        tenantContext.SetupGet(t => t.TenantId).Returns(activeTenantId);
        repository.Setup(r => r.GetParticipantProfilesAsync(It.IsAny<IReadOnlySet<Guid>>()))
            .ReturnsAsync(new Dictionary<Guid, ParticipantProfile>
            {
                [venueTenantId] = ParticipantProfile.Create(
                    venueTenantId,
                    "The Roundhouse",
                    "Greater London",
                    "London")
            });
        var service = new MessageService(
            repository.Object, Mock.Of<IConversationsNotifier>(), Mock.Of<IBus>(), new InlineOutboxBehavior(),
            currentUser.Object, tenantContext.Object,
            Mock.Of<ITenantModule>(), Mock.Of<IUserModule>(), TimeProvider.System);

        var previews = await service.GetRecentPreviewsAsync();

        var preview = Assert.Single(previews);
        Assert.Equal("The Roundhouse", preview.OtherPartyName);
        Assert.Equal("See you Friday", preview.Preview);
        Assert.True(preview.Unread);
        Assert.Equal("/_artist/?inbox=open", preview.Href);
    }

    [Fact]
    public async Task SendAndNotify_FansOutOneNotificationPerRecipientTenantMember()
    {
        var venueTenantId = Guid.NewGuid();
        var artistTenantId = Guid.NewGuid();
        var sentByUserId = Guid.NewGuid();
        var recipientMembers = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        MessageDto? payload = null;

        this.repository.Setup(r => r.AddAsync(It.IsAny<MessageEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MessageEntity message, CancellationToken _) => message);
        this.repository.Setup(r => r.GetParticipantProfilesAsync(It.IsAny<IReadOnlySet<Guid>>()))
            .ReturnsAsync(new Dictionary<Guid, ParticipantProfile>
            {
                [venueTenantId] = ParticipantProfile.Create(
                    venueTenantId, "The Roundhouse", "Greater London", "London")
            });

        this.notifier.Setup(n => n.MessageReceivedAsync(It.IsAny<string>(), It.IsAny<object>()))
            .Callback<string, object>((_, value) => payload = Assert.IsType<MessageDto>(value))
            .Returns(Task.CompletedTask);

        this.tenantModule.Setup(t => t.GetMemberUserIdsAsync(artistTenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(recipientMembers);

        await this.sut.SendAndNotifyAsync(venueTenantId, artistTenantId,
            senderTenantId: venueTenantId, sentByUserId: sentByUserId, "hello", MessageAction.ApplicationAccepted);

        this.repository.Verify(r => r.AddAsync(It.IsAny<MessageEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        this.repository.Verify(r => r.InsertAsync(It.IsAny<MessageEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        this.tenantModule.Verify(t => t.GetMemberUserIdsAsync(artistTenantId, It.IsAny<CancellationToken>()), Times.Once);
        this.bus.Verify(b => b.PublishAsync(
            It.Is<TenantActivityRecordedEvent>(e =>
                e.Activity.TenantId == artistTenantId &&
                e.Activity.Type == ActivityType.ApplicationAccepted &&
                e.Activity.Subject == "hello" &&
                e.Activity.Url == "/_artist/?inbox=open"),
            It.IsAny<CancellationToken>()),
            Times.Once);
        foreach (var member in recipientMembers)
            this.notifier.Verify(n => n.MessageReceivedAsync(member.ToString(), It.IsAny<object>()), Times.Once);
        this.notifier.VerifyNoOtherCalls();
        Assert.NotNull(payload);
        Assert.Equal(venueTenantId, payload.CounterpartTenantId);
        Assert.Equal(MessageSenderKind.Org, payload.Sender.Kind);
        Assert.Equal("The Roundhouse", payload.Sender.DisplayName);
        Assert.Equal("Greater London", payload.Sender.County);
        Assert.Equal("London", payload.Sender.Town);
    }

    [Fact]
    public async Task GetInbox_ProjectedParticipantProfile_ReturnsProfileSender()
    {
        var venueTenantId = Guid.NewGuid();
        var artistTenantId = Guid.NewGuid();
        var message = MessageEntity.Create(
            venueTenantId, artistTenantId, artistTenantId, Guid.NewGuid(), "hello", DateTime.UtcNow);
        this.tenantContext.SetupGet(t => t.TenantId).Returns(venueTenantId);
        this.repository.Setup(r => r.GetByTenantIdAsync(venueTenantId, It.IsAny<IPageParams>()))
            .ReturnsAsync(new Pagination<MessageEntity>([message], 1, 1, 10));
        this.repository.Setup(r => r.GetParticipantProfilesAsync(It.IsAny<IReadOnlySet<Guid>>()))
            .ReturnsAsync(new Dictionary<Guid, ParticipantProfile>
            {
                [artistTenantId] = ParticipantProfile.Create(artistTenantId, "Artist", "Kent", "Deal")
            });

        var result = await this.sut.GetInboxAsync(Mock.Of<IPageParams>());

        var dto = Assert.Single(result.Data);
        Assert.Equal(artistTenantId, dto.CounterpartTenantId);
        Assert.Equal(MessageSenderKind.Org, dto.Sender.Kind);
        Assert.Equal("Artist", dto.Sender.DisplayName);
        Assert.Equal("Kent", dto.Sender.County);
        Assert.Equal("Deal", dto.Sender.Town);
    }

    [Fact]
    public async Task GetInbox_MissingParticipantProfile_ReturnsUnknownSender()
    {
        var venueTenantId = Guid.NewGuid();
        var artistTenantId = Guid.NewGuid();
        var message = MessageEntity.Create(
            venueTenantId, artistTenantId, artistTenantId, Guid.NewGuid(), "hello", DateTime.UtcNow);
        this.tenantContext.SetupGet(t => t.TenantId).Returns(venueTenantId);
        this.repository.Setup(r => r.GetByTenantIdAsync(venueTenantId, It.IsAny<IPageParams>()))
            .ReturnsAsync(new Pagination<MessageEntity>([message], 1, 1, 10));
        this.repository.Setup(r => r.GetParticipantProfilesAsync(It.IsAny<IReadOnlySet<Guid>>()))
            .ReturnsAsync(new Dictionary<Guid, ParticipantProfile>());

        var result = await this.sut.GetInboxAsync(Mock.Of<IPageParams>());

        var sender = Assert.Single(result.Data).Sender;
        Assert.Equal(MessageSenderKind.Org, sender.Kind);
        Assert.Equal("Unknown", sender.DisplayName);
        Assert.Null(sender.County);
        Assert.Null(sender.Town);
    }

    private sealed class InlineOutboxBehavior : IOutboxUnitOfWorkBehavior
    {
        public Task ExecuteAsync(Func<Task> action, CancellationToken cancellationToken = default) =>
            action();

        public Task<T> ExecuteAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken = default) =>
            action();
    }
}
