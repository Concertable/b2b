using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Infrastructure.Services;
using Moq;

namespace Concertable.B2B.Concert.UnitTests.Services;

public sealed class ConcertTenantDeletionGuardTests
{
    private readonly Guid tenantId = Guid.NewGuid();
    private readonly List<string> steps = [];
    private readonly Mock<IConcertPrivilegedRepository> concertPrivilegedRepository = new();
    private readonly Mock<IInvoicePrivilegedRepository> invoicePrivilegedRepository = new();
    private readonly Mock<ISelfBillingAgreementRepository> selfBillingAgreementRepository = new();

    [Theory]
    [InlineData(true, false, false, true)]
    [InlineData(false, true, false, true)]
    [InlineData(false, false, true, true)]
    [InlineData(false, false, false, false)]
    public async Task HasLiveObligations_TakesTheTenantAgreementsLockBeforeCheckingTenantRows(
        bool hasConcert,
        bool hasInvoice,
        bool hasAgreement,
        bool expected)
    {
        selfBillingAgreementRepository
            .Setup(value => value.AcquireTenantAgreementsExclusiveLockAsync(tenantId, It.IsAny<CancellationToken>()))
            .Callback(() => steps.Add("lock"))
            .Returns(Task.CompletedTask);
        concertPrivilegedRepository
            .Setup(value => value.ExistsByTenantIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .Callback(() => steps.Add("concerts"))
            .ReturnsAsync(hasConcert);
        invoicePrivilegedRepository
            .Setup(value => value.ExistsByTenantIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .Callback(() => steps.Add("invoices"))
            .ReturnsAsync(hasInvoice);
        selfBillingAgreementRepository
            .Setup(value => value.ExistsByTenantIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .Callback(() => steps.Add("agreements"))
            .ReturnsAsync(hasAgreement);
        var guard = new ConcertTenantDeletionGuard(
            concertPrivilegedRepository.Object,
            invoicePrivilegedRepository.Object,
            selfBillingAgreementRepository.Object);

        var result = await guard.HasLiveObligationsAsync(tenantId);

        Assert.Equal(expected, result);
        Assert.Equal("lock", steps[0]);
    }
}
