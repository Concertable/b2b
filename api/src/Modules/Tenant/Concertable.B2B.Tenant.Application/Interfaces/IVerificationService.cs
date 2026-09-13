using Concertable.B2B.Tenant.Application.DTOs;
using Concertable.B2B.Tenant.Application.Requests;

namespace Concertable.B2B.Tenant.Application.Interfaces;

internal interface IVerificationService
{
    /// <summary>The active tenant's own verification status, reason and evidence — <see cref="Option{T}.None"/>
    /// when the tenant has never submitted (fail-closed: no row means not verified).</summary>
    Task<Option<VerificationStatusDto>> GetStatusAsync(CancellationToken ct = default);

    /// <summary>Submits or resubmits evidence for the active tenant. Creates the first row when none exists;
    /// otherwise only legal while <see cref="Domain.Enums.TenantVerificationStatus.Rejected"/>.</summary>
    Task<Result<VerificationStatusDto, SubmitVerificationError>> SubmitAsync(
        IReadOnlyList<EvidenceUpload> uploads,
        CancellationToken ct = default);

    /// <summary>Whether the given tenant is <see cref="Domain.Enums.TenantVerificationStatus.Approved"/> — false
    /// when the tenant has never submitted (fail-closed) or is <c>Pending</c>/<c>Rejected</c>. Cross-module
    /// consumers reach this through <see cref="Contracts.ITenantModule.IsVerifiedAsync"/>.</summary>
    Task<bool> IsVerifiedAsync(Guid tenantId, CancellationToken ct = default);

    /// <summary>The platform-admin review queue: every <see cref="Domain.Enums.TenantVerificationStatus.Pending"/>
    /// submission, enriched with the owning venue/artist's contact.</summary>
    Task<IPagination<PendingVerificationDto>> GetPendingAsync(IPageParams pageParams, CancellationToken ct = default);

    /// <summary>Admin approval of a tenant's pending submission. Only legal while
    /// <see cref="Domain.Enums.TenantVerificationStatus.Pending"/>; notifies the tenant's contact on success.</summary>
    Task<UnitResult<VerificationReviewError>> ApproveAsync(Guid tenantId, CancellationToken ct = default);

    /// <summary>Admin rejection of a tenant's pending submission, with a reason. Only legal while
    /// <see cref="Domain.Enums.TenantVerificationStatus.Pending"/>; notifies the tenant's contact on success.</summary>
    Task<UnitResult<VerificationReviewError>> RejectAsync(Guid tenantId, string reason, CancellationToken ct = default);
}
