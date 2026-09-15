using Concertable.B2B.Application.Contracts;
using Concertable.B2B.Booking.Contracts;
using Concertable.B2B.Concert.Contracts;
using Concertable.B2B.Privacy.Infrastructure.Services;
using Concertable.B2B.Tenant.Contracts;
using Concertable.B2B.Tenant.Contracts.Enums;
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
            tenantModule.Object,
            applicationModule.Object,
            bookingModule.Object,
            concertModule.Object);
    }

    [Fact]
    public async Task HasLiveObligationsAsync_NoMemberships_ReturnsFalseWithoutQueryingOwners()
    {
        var subjectId = Guid.NewGuid();
        tenantModule.Setup(m => m.GetMembershipsAsync(subjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await obligationChecker.HasLiveObligationsAsync(subjectId);

        Assert.False(result);
        applicationModule.Verify(
            m => m.GetLiveObligationCountAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()),
            Times.Never);
        bookingModule.Verify(
            m => m.GetLiveObligationCountAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()),
            Times.Never);
        concertModule.Verify(
            m => m.GetLiveObligationCountAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()),
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

        Assert.True(await obligationChecker.HasLiveObligationsAsync(subjectId));
    }

    [Fact]
    public async Task HasLiveObligationsAsync_NoOwningModuleReportsAnObligation_ReturnsFalse()
    {
        var subjectId = Guid.NewGuid();
        SetupSingleMembership(subjectId);
        SetupObligations(application: false, booking: false, concert: false);

        Assert.False(await obligationChecker.HasLiveObligationsAsync(subjectId));
    }

    private void SetupSingleMembership(Guid subjectId) =>
        tenantModule.Setup(m => m.GetMembershipsAsync(subjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new MembershipDto(Guid.NewGuid(), "Acme", TenantType.Venue, TenantRole.Owner)]);

    private void SetupObligations(bool application, bool booking, bool concert)
    {
        applicationModule.Setup(m => m.GetLiveObligationCountAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(application ? 1 : 0);
        bookingModule.Setup(m => m.GetLiveObligationCountAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking ? 1 : 0);
        concertModule.Setup(m => m.GetLiveObligationCountAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(concert ? 1 : 0);
    }
}
