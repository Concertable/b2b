using Concertable.B2B.Concert.Domain.Events;
using Concertable.B2B.Concert.Infrastructure.Events;
using Concertable.B2B.Tenant.Contracts;
using Concertable.B2B.User.Contracts;
using Concertable.Kernel.Identity;
using Concertable.Messaging.Contracts;
using Concertable.Shared.Email.Application;
using Moq;

namespace Concertable.B2B.Concert.UnitTests.Events;

public sealed class ApplicationCounterpartyNotifiedDomainEventHandlerTests
{
    [Fact]
    public async Task HandleAsync_StagesOneEmailPerRecipientMember_WithKindCopy()
    {
        var recipientTenantId = Guid.NewGuid();
        var member1 = Guid.NewGuid();
        var member2 = Guid.NewGuid();
        var staged = new List<SendEmailCommand>();
        var sut = CreateHandler(recipientTenantId, staged, new Dictionary<Guid, string>
        {
            [member1] = "one@example.com",
            [member2] = "two@example.com",
        });

        await sut.HandleAsync(new ApplicationCounterpartyNotifiedDomainEvent(
            recipientTenantId, ApplicationNotification.Cancelled));

        Assert.Equal(2, staged.Count);
        Assert.All(staged, c => Assert.Equal("Concert Application Cancelled", c.Subject));
        Assert.All(staged, c => Assert.Equal(
            "Your accepted application has been cancelled. Any payment made towards it has been refunded.", c.Body));
        Assert.Contains(staged, c => c.To == "one@example.com");
        Assert.Contains(staged, c => c.To == "two@example.com");
    }

    [Fact]
    public async Task HandleAsync_AppliedCopy_IncludesActingUserEmail()
    {
        var recipientTenantId = Guid.NewGuid();
        var member = Guid.NewGuid();
        var staged = new List<SendEmailCommand>();
        var sut = CreateHandler(recipientTenantId, staged,
            new Dictionary<Guid, string> { [member] = "venue@example.com" },
            actingUserEmail: "artist@example.com");

        await sut.HandleAsync(new ApplicationCounterpartyNotifiedDomainEvent(
            recipientTenantId, ApplicationNotification.Applied));

        var email = Assert.Single(staged);
        Assert.Equal("venue@example.com", email.To);
        Assert.Equal("Concert Application", email.Subject);
        Assert.Equal("artist@example.com has applied to your concert opportunity", email.Body);
    }

    private static ApplicationCounterpartyNotifiedDomainEventHandler CreateHandler(
        Guid recipientTenantId,
        List<SendEmailCommand> staged,
        IReadOnlyDictionary<Guid, string> memberEmails,
        string? actingUserEmail = null)
    {
        var tenantModule = new Mock<ITenantModule>();
        tenantModule.Setup(t => t.GetMemberUserIdsAsync(recipientTenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(memberEmails.Keys.ToList());

        var userModule = new Mock<IUserModule>();
        userModule.Setup(u => u.GetEmailsByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(memberEmails);

        var currentUser = new Mock<ICurrentUser>();
        currentUser.Setup(u => u.Email).Returns(actingUserEmail);

        var bus = new Mock<IBus>();
        bus.Setup(b => b.SendAsync(It.IsAny<SendEmailCommand>(), It.IsAny<CancellationToken>()))
            .Callback<SendEmailCommand, CancellationToken>((c, _) => staged.Add(c))
            .Returns(Task.CompletedTask);

        return new ApplicationCounterpartyNotifiedDomainEventHandler(
            tenantModule.Object, userModule.Object, currentUser.Object, bus.Object);
    }
}
