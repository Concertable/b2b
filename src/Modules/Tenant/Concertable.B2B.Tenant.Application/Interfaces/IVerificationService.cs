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
}
