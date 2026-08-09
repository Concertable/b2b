using Concertable.B2B.Artist.Contracts;
using Concertable.B2B.Conversations.Application.Interfaces;
using Concertable.B2B.Conversations.Infrastructure.Services;
using Concertable.B2B.Tenant.Contracts;
using Concertable.B2B.User.Contracts;
using Concertable.B2B.Venue.Contracts;
using Concertable.Kernel.Identity;
using Reunion;
using Moq;

namespace Concertable.B2B.Conversations.UnitTests.Services;

public sealed class MessageServiceTests
{
    [Fact]
    public async Task SendAndNotify_FansOutOneNotificationPerRecipientTenantMember()
    {
        var venueTenantId = Guid.NewGuid();
        var artistTenantId = Guid.NewGuid();
        var sentByUserId = Guid.NewGuid();
        var recipientMembers = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };

        var repository = new Mock<IMessageRepository>();
        repository.Setup(r => r.AddAsync(It.IsAny<MessageEntity>())).Returns(Task.CompletedTask);
        repository.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var notifier = new Mock<IConversationsNotifier>();
        notifier.Setup(n => n.MessageReceivedAsync(It.IsAny<string>(), It.IsAny<object>())).Returns(Task.CompletedTask);

        // Sender is the venue side, so the recipient tenant is the artist side.
        var tenantModule = new Mock<ITenantModule>();
        tenantModule.Setup(t => t.GetMemberUserIdsAsync(artistTenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(recipientMembers);

        var venueModule = new Mock<IVenueModule>();
        venueModule.Setup(v => v.GetOrgIdentityByTenantIdAsync(venueTenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Option.Some(new VenueOrgIdentity("The Roundhouse", "Greater London", "London")));

        var service = new MessageService(
            repository.Object, notifier.Object, Mock.Of<ICurrentUser>(), Mock.Of<ITenantContext>(),
            tenantModule.Object, Mock.Of<IUserModule>(), venueModule.Object, Mock.Of<IArtistModule>(), TimeProvider.System);

        await service.SendAndNotifyAsync(venueTenantId, artistTenantId,
            senderTenantId: venueTenantId, sentByUserId: sentByUserId, "hello", MessageAction.ApplicationAccepted);

        tenantModule.Verify(t => t.GetMemberUserIdsAsync(artistTenantId, It.IsAny<CancellationToken>()), Times.Once);
        foreach (var member in recipientMembers)
            notifier.Verify(n => n.MessageReceivedAsync(member.ToString(), It.IsAny<object>()), Times.Once);
        notifier.VerifyNoOtherCalls();
    }
}
