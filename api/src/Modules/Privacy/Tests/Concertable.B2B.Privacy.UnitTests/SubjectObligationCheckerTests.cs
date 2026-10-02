using Concertable.B2B.Authorization.Contracts.Enums;
using Concertable.B2B.Application.Contracts;
using Concertable.B2B.Booking.Contracts;
using Concertable.B2B.Concert.Contracts;
using Concertable.B2B.Privacy.Infrastructure.Services;
using Concertable.B2B.Tenant.Contracts;
using Moq;

namespace Concertable.B2B.Privacy.UnitTests;

public sealed class SubjectObligationCheckerTests
{
    private readonly Mock<ITenantModule> tenantModule = new();
    private readonly Mock<IApplicationModule> applicationModule = new();
    private readonly Mock<IBookingModule> bookingModule = new();
    private readonly Mock<IConcertModule> concertModule = new();
    private readonly SubjectObligationChecker obligationChecker;

    public SubjectObligationCheckerTests()
    {
        this.obligationChecker = new SubjectObligationChecker(
            this.tenantModule.Object,
            this.applicationModule.Object,
            this.bookingModule.Object,
            this.concertModule.Object);
    }

    [Fact]
    public async Task HasLiveObligationsAsync_NoMemberships_ReturnsFalseWithoutQueryingOwners()
    {
        var subjectId = Guid.NewGuid();
        this.tenantModule.Setup(m => m.GetMembershipsAsync(subjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await this.obligationChecker.HasLiveObligationsAsync(subjectId, new HashSet<Guid>());

        Assert.False(result);
        this.applicationModule.Verify(
            m => m.HasLiveObligationsByTenantIdsAsync(It.IsAny<IReadOnlySet<Guid>>(), It.IsAny<CancellationToken>()),
            Times.Never);
        this.bookingModule.Verify(
            m => m.HasLiveObligationsByTenantIdsAsync(It.IsAny<IReadOnlySet<Guid>>(), It.IsAny<CancellationToken>()),
            Times.Never);
        this.concertModule.Verify(
            m => m.HasLiveObligationsByTenantIdsAsync(It.IsAny<IReadOnlySet<Guid>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(false, false, true)]
    public async Task HasLiveObligationsAsync_AnyOwningModuleReportsAnObligation_ReturnsTrue(
        bool application,
        bool booking,
        bool concert)
    {
        var subjectId = Guid.NewGuid();
        SetupSingleMembership(subjectId);
        SetupObligations(application, booking, concert);

        Assert.True(await this.obligationChecker.HasLiveObligationsAsync(subjectId, new HashSet<Guid>()));
    }

    [Fact]
    public async Task HasLiveObligationsAsync_NoOwningModuleReportsAnObligation_ReturnsFalse()
    {
        var subjectId = Guid.NewGuid();
        SetupSingleMembership(subjectId);
        SetupObligations(application: false, booking: false, concert: false);

        Assert.False(await this.obligationChecker.HasLiveObligationsAsync(subjectId, new HashSet<Guid>()));
    }

    [Fact]
    public async Task HasLiveObligationsAsync_MembershipsAlreadySevered_ChecksTheCapturedTenantScope()
    {
        var subjectId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        this.tenantModule.Setup(t => t.GetMembershipsAsync(subjectId, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        this.bookingModule.Setup(b => b.HasLiveObligationsByTenantIdsAsync(
            It.Is<IReadOnlySet<Guid>>(ids => ids.SetEquals(new[] { tenantId })), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        Assert.True(await this.obligationChecker.HasLiveObligationsAsync(subjectId, new HashSet<Guid> { tenantId }));
    }

    private void SetupSingleMembership(Guid subjectId) =>
        this.tenantModule.Setup(m => m.GetMembershipsAsync(subjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new MembershipDto(Guid.NewGuid(), Guid.NewGuid(), "Acme", TenantRole.Owner, 1, [], [])]);

    private void SetupObligations(bool application, bool booking, bool concert)
    {
        this.applicationModule.Setup(m => m.HasLiveObligationsByTenantIdsAsync(It.IsAny<IReadOnlySet<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(application);
        this.bookingModule.Setup(m => m.HasLiveObligationsByTenantIdsAsync(It.IsAny<IReadOnlySet<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);
        this.concertModule.Setup(m => m.HasLiveObligationsByTenantIdsAsync(It.IsAny<IReadOnlySet<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(concert);
    }
}
