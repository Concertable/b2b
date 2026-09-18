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
    private const int ThreadId = 7;

    private readonly Mock<IMessageRepository> repository;
    private readonly Mock<IThreadRepository> threadRepository;
    private readonly Mock<IConversationsNotifier> notifier;
    private readonly Mock<IBus> bus;
    private readonly Mock<ITenantContext> tenantContext;
    private readonly Mock<ITenantModule> tenantModule;
    private readonly MessageService sut;

    public MessageServiceTests()
    {
        this.repository = new Mock<IMessageRepository>();
        this.threadRepository = new Mock<IThreadRepository>();
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
            this.threadRepository.Object,
            this.notifier.Object,
            this.bus.Object,
            new InlineOutboxBehavior(),
            Mock.Of<ICurrentUser>(),
            this.tenantContext.Object,
            this.tenantModule.Object,
            Mock.Of<IUserModule>(),
            TimeProvider.System);
    }

    #region GetRecentPreviewsAsync

    [Fact]
    public async Task GetRecentPreviewsAsync_ResolvesCounterpartyIdentityAndInboxHref()
    {
        var activeTenantId = Guid.NewGuid();
        var counterpartTenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var at = new DateTime(2026, 8, 14, 12, 0, 0, DateTimeKind.Utc);
        var previews = new Mock<IMessageRepository>();
        previews.Setup(r => r.GetRecentPreviewsAsync(activeTenantId, userId))
            .ReturnsAsync([new(12, ThreadId, counterpartTenantId, "See you Friday", at, true)]);
        previews.Setup(r => r.GetParticipantProfilesAsync(It.IsAny<IReadOnlySet<Guid>>()))
            .ReturnsAsync(new Dictionary<Guid, ParticipantProfile>
            {
                [counterpartTenantId] = ParticipantProfile.Create(
                    counterpartTenantId,
                    "The Roundhouse",
                    "Greater London",
                    "London")
            });
        var currentUser = new Mock<ICurrentUser>();
        currentUser.SetupGet(u => u.Id).Returns(userId);
        var activeTenant = new Mock<ITenantContext>();
        activeTenant.SetupGet(t => t.TenantId).Returns(activeTenantId);
        var service = new MessageService(
            previews.Object, this.threadRepository.Object, Mock.Of<IConversationsNotifier>(), Mock.Of<IBus>(),
            new InlineOutboxBehavior(), currentUser.Object, activeTenant.Object,
            Mock.Of<ITenantModule>(), Mock.Of<IUserModule>(), TimeProvider.System);

        var result = await service.GetRecentPreviewsAsync();

        var preview = Assert.Single(result);
        Assert.Equal("The Roundhouse", preview.OtherPartyName);
        Assert.Equal("See you Friday", preview.Preview);
        Assert.True(preview.Unread);
        Assert.Equal("/?inbox=open", preview.Href);
    }

    #endregion

    #region SendAndNotifyAsync

    [Fact]
    public async Task SendAndNotifyAsync_FansOutOneNotificationPerRecipientTenantMember()
    {
        var senderTenantId = Guid.NewGuid();
        var recipientTenantId = Guid.NewGuid();
        var sentByUserId = Guid.NewGuid();
        var recipientMembers = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        MessageDto? payload = null;

        this.threadRepository
            .Setup(t => t.GetByParticipantsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ThreadEntity.Create([senderTenantId, recipientTenantId], DateTime.UnixEpoch));
        this.threadRepository
            .Setup(t => t.GetParticipantTenantIdsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([senderTenantId, recipientTenantId]);
        this.repository.Setup(r => r.AddAsync(It.IsAny<MessageEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MessageEntity message, CancellationToken _) => message);
        this.repository.Setup(r => r.GetParticipantProfilesAsync(It.IsAny<IReadOnlySet<Guid>>()))
            .ReturnsAsync(new Dictionary<Guid, ParticipantProfile>
            {
                [senderTenantId] = ParticipantProfile.Create(
                    senderTenantId, "The Roundhouse", "Greater London", "London")
            });
        this.notifier.Setup(n => n.MessageReceivedAsync(It.IsAny<string>(), It.IsAny<object>()))
            .Callback<string, object>((_, value) => payload = Assert.IsType<MessageDto>(value))
            .Returns(Task.CompletedTask);
        this.tenantModule.Setup(t => t.GetMemberUserIdsAsync(recipientTenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(recipientMembers);

        await this.sut.SendAndNotifyAsync(
            [senderTenantId, recipientTenantId],
            senderTenantId,
            sentByUserId,
            "hello",
            MessageAction.ApplicationAccepted);

        this.repository.Verify(r => r.AddAsync(It.IsAny<MessageEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        this.tenantModule.Verify(t => t.GetMemberUserIdsAsync(recipientTenantId, It.IsAny<CancellationToken>()), Times.Once);
        this.bus.Verify(b => b.PublishAsync(
            It.Is<TenantActivityRecordedEvent>(e =>
                e.Activity.TenantId == recipientTenantId &&
                e.Activity.Type == ActivityType.ApplicationAccepted &&
                e.Activity.Subject == "hello" &&
                e.Activity.Url == "/?inbox=open"),
            It.IsAny<CancellationToken>()),
            Times.Once);
        foreach (var member in recipientMembers)
            this.notifier.Verify(n => n.MessageReceivedAsync(member.ToString(), It.IsAny<object>()), Times.Once);
        this.notifier.VerifyNoOtherCalls();
        Assert.NotNull(payload);
        Assert.Equal(MessageSenderKind.Org, payload.Sender.Kind);
        Assert.Equal("The Roundhouse", payload.Sender.DisplayName);
    }

    [Fact]
    public async Task SendAndNotifyAsync_NoThreadYet_OpensOne()
    {
        var senderTenantId = Guid.NewGuid();
        var recipientTenantId = Guid.NewGuid();

        this.threadRepository
            .Setup(t => t.GetByParticipantsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ThreadEntity?)null);
        this.threadRepository
            .Setup(t => t.GetParticipantTenantIdsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([senderTenantId, recipientTenantId]);
        this.repository.Setup(r => r.AddAsync(It.IsAny<MessageEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MessageEntity message, CancellationToken _) => message);
        this.repository.Setup(r => r.GetParticipantProfilesAsync(It.IsAny<IReadOnlySet<Guid>>()))
            .ReturnsAsync(new Dictionary<Guid, ParticipantProfile>());
        this.tenantModule.Setup(t => t.GetMemberUserIdsAsync(recipientTenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        await this.sut.SendAndNotifyAsync(
            [senderTenantId, recipientTenantId],
            senderTenantId,
            Guid.NewGuid(),
            "hello");

        this.threadRepository.Verify(
            t => t.InsertAsync(It.IsAny<ThreadEntity>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region GetInboxAsync

    [Fact]
    public async Task GetInboxAsync_ProjectedParticipantProfile_ReturnsProfileSender()
    {
        var activeTenantId = Guid.NewGuid();
        var counterpartTenantId = Guid.NewGuid();
        var message = MessageEntity.Create(
            ThreadId, counterpartTenantId, Guid.NewGuid(), "hello", DateTime.UtcNow);
        this.tenantContext.SetupGet(t => t.TenantId).Returns(activeTenantId);
        this.threadRepository
            .Setup(t => t.GetParticipantTenantIdsAsync(ThreadId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([activeTenantId, counterpartTenantId]);
        this.repository.Setup(r => r.GetByTenantIdAsync(activeTenantId, It.IsAny<IPageParams>()))
            .ReturnsAsync(new Pagination<MessageEntity>([message], 1, 1, 10));
        this.repository.Setup(r => r.GetParticipantProfilesAsync(It.IsAny<IReadOnlySet<Guid>>()))
            .ReturnsAsync(new Dictionary<Guid, ParticipantProfile>
            {
                [counterpartTenantId] = ParticipantProfile.Create(counterpartTenantId, "Artist", "Kent", "Deal")
            });

        var result = await this.sut.GetInboxAsync(Mock.Of<IPageParams>());

        var dto = Assert.Single(result.Data);
        Assert.Equal(counterpartTenantId, dto.CounterpartTenantId);
        Assert.Equal(MessageSenderKind.Org, dto.Sender.Kind);
        Assert.Equal("Artist", dto.Sender.DisplayName);
        Assert.Equal("Kent", dto.Sender.County);
        Assert.Equal("Deal", dto.Sender.Town);
    }

    [Fact]
    public async Task GetInboxAsync_MissingParticipantProfile_ReturnsUnknownSender()
    {
        var activeTenantId = Guid.NewGuid();
        var counterpartTenantId = Guid.NewGuid();
        var message = MessageEntity.Create(
            ThreadId, counterpartTenantId, Guid.NewGuid(), "hello", DateTime.UtcNow);
        this.tenantContext.SetupGet(t => t.TenantId).Returns(activeTenantId);
        this.threadRepository
            .Setup(t => t.GetParticipantTenantIdsAsync(ThreadId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([activeTenantId, counterpartTenantId]);
        this.repository.Setup(r => r.GetByTenantIdAsync(activeTenantId, It.IsAny<IPageParams>()))
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

    #endregion

    private sealed class InlineOutboxBehavior : IOutboxUnitOfWorkBehavior
    {
        public Task ExecuteAsync(Func<Task> action, CancellationToken cancellationToken = default) =>
            action();

        public Task<T> ExecuteAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken = default) =>
            action();
    }
}
