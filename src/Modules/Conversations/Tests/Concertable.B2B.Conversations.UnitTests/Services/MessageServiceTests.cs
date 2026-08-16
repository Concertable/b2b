using Concertable.B2B.Artist.Contracts;
using Concertable.B2B.Conversations.Application.Interfaces;
using Concertable.B2B.Conversations.Infrastructure;
using Concertable.B2B.Conversations.Infrastructure.Services;
using Concertable.B2B.Tenant.Contracts;
using Concertable.B2B.Tenant.Contracts.Events;
using Concertable.B2B.User.Contracts;
using Concertable.B2B.Venue.Contracts;
using Concertable.Kernel.Identity;
using Concertable.Messaging.Contracts;
using Reunion;
using Moq;

namespace Concertable.B2B.Conversations.UnitTests.Services;

public sealed class MessageServiceTests
{
    [Fact]
    public async Task GetRecentPreviews_ResolvesCounterpartyIdentityAndPersonaHref()
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
        var venueModule = new Mock<IVenueModule>();
        venueModule.Setup(v => v.GetOrgIdentityByTenantIdAsync(venueTenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VenueOrgIdentity("The Roundhouse", "Greater London", "London"));
        var service = new MessageService(
            repository.Object, Mock.Of<IConversationsNotifier>(), Mock.Of<IBus>(), new InlineOutboxBehavior(),
            currentUser.Object, tenantContext.Object,
            Mock.Of<ITenantModule>(), Mock.Of<IUserModule>(), venueModule.Object, Mock.Of<IArtistModule>(), TimeProvider.System);

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

        var repository = new Mock<IMessageRepository>();
        var bus = new Mock<IBus>();
        bus.Setup(b => b.PublishAsync(It.IsAny<TenantActivityRecordedEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        repository.Setup(r => r.AddAsync(It.IsAny<MessageEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MessageEntity message, CancellationToken _) => message);
        repository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var notifier = new Mock<IConversationsNotifier>();
        notifier.Setup(n => n.MessageReceivedAsync(It.IsAny<string>(), It.IsAny<object>())).Returns(Task.CompletedTask);

        var tenantModule = new Mock<ITenantModule>();
        tenantModule.Setup(t => t.GetMemberUserIdsAsync(artistTenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(recipientMembers);

        var venueModule = new Mock<IVenueModule>();
        venueModule.Setup(v => v.GetOrgIdentityByTenantIdAsync(venueTenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Option.Some(new VenueOrgIdentity("The Roundhouse", "Greater London", "London")));

        var service = new MessageService(
            repository.Object, notifier.Object, bus.Object, new InlineOutboxBehavior(),
            Mock.Of<ICurrentUser>(), Mock.Of<ITenantContext>(),
            tenantModule.Object, Mock.Of<IUserModule>(), venueModule.Object, Mock.Of<IArtistModule>(), TimeProvider.System);

        await service.SendAndNotifyAsync(venueTenantId, artistTenantId,
            senderTenantId: venueTenantId, sentByUserId: sentByUserId, "hello", MessageAction.ApplicationAccepted);

        tenantModule.Verify(t => t.GetMemberUserIdsAsync(artistTenantId, It.IsAny<CancellationToken>()), Times.Once);
        bus.Verify(b => b.PublishAsync(
            It.Is<TenantActivityRecordedEvent>(e =>
                e.Activity.TenantId == artistTenantId &&
                e.Activity.Type == ActivityType.ApplicationAccepted &&
                e.Activity.Subject == "hello" &&
                e.Activity.Url == "/_artist/?inbox=open"),
            It.IsAny<CancellationToken>()),
            Times.Once);
        foreach (var member in recipientMembers)
            notifier.Verify(n => n.MessageReceivedAsync(member.ToString(), It.IsAny<object>()), Times.Once);
        notifier.VerifyNoOtherCalls();
    }

    private sealed class InlineOutboxBehavior : IOutboxUnitOfWorkBehavior
    {
        public Task ExecuteAsync(Func<Task> action, CancellationToken cancellationToken = default) =>
            action();

        public Task<T> ExecuteAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken = default) =>
            action();
    }
}
