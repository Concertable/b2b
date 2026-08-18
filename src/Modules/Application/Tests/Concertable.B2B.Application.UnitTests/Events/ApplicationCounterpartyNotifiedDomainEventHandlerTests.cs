using Concertable.B2B.Application.Domain.Events;
using Concertable.B2B.Application.Infrastructure.Events;
using Concertable.B2B.Tenant.Contracts;
using Concertable.B2B.User.Contracts;
using Concertable.Kernel.Identity;
using Concertable.Messaging.Contracts;
using Concertable.Shared.Email.Application;
using Moq;

namespace Concertable.B2B.Application.UnitTests;

public sealed class ApplicationCounterpartyNotifiedDomainEventHandlerTests
{
    private readonly Guid recipientTenantId;
    private readonly Dictionary<Guid, string> memberEmails;
    private readonly List<SendEmailCommand> staged;
    private readonly ApplicationCounterpartyNotifiedDomainEventHandler sut;

    public ApplicationCounterpartyNotifiedDomainEventHandlerTests()
    {
        this.recipientTenantId = Guid.NewGuid();
        this.memberEmails = new Dictionary<Guid, string>
        {
            [Guid.NewGuid()] = "one@example.com",
            [Guid.NewGuid()] = "two@example.com",
        };
        this.staged = [];

        var tenantModule = new Mock<ITenantModule>();
        tenantModule.Setup(t => t.GetMemberUserIdsAsync(this.recipientTenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => this.memberEmails.Keys.ToList());

        var userModule = new Mock<IUserModule>();
        userModule.Setup(u => u.GetEmailsByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(() => this.memberEmails);

        var currentUser = new Mock<ICurrentUser>();
        currentUser.Setup(u => u.Email).Returns("artist@example.com");

        var bus = new Mock<IBus>();
        bus.Setup(b => b.SendAsync(It.IsAny<SendEmailCommand>(), It.IsAny<CancellationToken>()))
            .Callback<SendEmailCommand, CancellationToken>((command, _) => this.staged.Add(command))
            .Returns(Task.CompletedTask);

        this.sut = new ApplicationCounterpartyNotifiedDomainEventHandler(
            tenantModule.Object,
            userModule.Object,
            currentUser.Object,
            bus.Object);
    }

    [Fact]
    public async Task HandleAsync_Accepted_StagesOneEmailPerRecipientMember()
    {
        await sut.HandleAsync(new ApplicationCounterpartyNotifiedDomainEvent(
            recipientTenantId,
            ApplicationNotification.Accepted));

        Assert.Equal(2, staged.Count);
        Assert.All(staged, command => Assert.Equal("Concert Application Accepted", command.Subject));
        Assert.All(staged, command => Assert.Equal(
            "Your application was accepted! A concert has been scheduled for you.",
            command.Body));
        Assert.Contains(staged, command => command.To == "one@example.com");
        Assert.Contains(staged, command => command.To == "two@example.com");
    }

    [Fact]
    public async Task HandleAsync_Applied_IncludesActingUserEmail()
    {
        memberEmails.Clear();
        memberEmails.Add(Guid.NewGuid(), "venue@example.com");

        await sut.HandleAsync(new ApplicationCounterpartyNotifiedDomainEvent(
            recipientTenantId,
            ApplicationNotification.Applied));

        var email = Assert.Single(staged);
        Assert.Equal("venue@example.com", email.To);
        Assert.Equal("Concert Application", email.Subject);
        Assert.Equal("artist@example.com has applied to your concert opportunity", email.Body);
    }
}
