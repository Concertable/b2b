using Concertable.B2B.Tenant.Application.DTOs;

namespace Concertable.B2B.Tenant.Application.Interfaces;

/// <summary>The platform-admin review surface over tenant verification — the pending queue and the
/// approve/reject actions. Tenant-facing submission lives on <see cref="IVerificationService"/>.</summary>
internal interface IVerificationAdminService
{
    Task<IPagination<PendingVerificationDto>> GetPendingAsync(IPageParams pageParams, CancellationToken ct = default);

    Task<UnitResult<VerificationReviewError>> ApproveAsync(Guid tenantId, CancellationToken ct = default);

    Task<UnitResult<VerificationReviewError>> RejectAsync(Guid tenantId, string reason, CancellationToken ct = default);
}
