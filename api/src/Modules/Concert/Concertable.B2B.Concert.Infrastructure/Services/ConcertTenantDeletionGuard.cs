using Concertable.B2B.Tenant.Contracts;

namespace Concertable.B2B.Concert.Infrastructure.Services;

internal sealed class ConcertTenantDeletionGuard : ITenantDeletionGuard
{
    private readonly IConcertPrivilegedRepository concertPrivilegedRepository;
    private readonly IInvoicePrivilegedRepository invoicePrivilegedRepository;
    private readonly ISelfBillingAgreementRepository selfBillingAgreementRepository;

    public ConcertTenantDeletionGuard(
        IConcertPrivilegedRepository concertPrivilegedRepository,
        IInvoicePrivilegedRepository invoicePrivilegedRepository,
        ISelfBillingAgreementRepository selfBillingAgreementRepository)
    {
        this.concertPrivilegedRepository = concertPrivilegedRepository;
        this.invoicePrivilegedRepository = invoicePrivilegedRepository;
        this.selfBillingAgreementRepository = selfBillingAgreementRepository;
    }

    public async Task<bool> HasLiveObligationsAsync(Guid tenantId, CancellationToken ct = default)
    {
        await selfBillingAgreementRepository.AcquireTenantAgreementsExclusiveLockAsync(tenantId, ct);
        return await concertPrivilegedRepository.ExistsByTenantIdAsync(tenantId, ct)
            || await invoicePrivilegedRepository.ExistsByTenantIdAsync(tenantId, ct)
            || await selfBillingAgreementRepository.ExistsByTenantIdAsync(tenantId, ct);
    }
}
