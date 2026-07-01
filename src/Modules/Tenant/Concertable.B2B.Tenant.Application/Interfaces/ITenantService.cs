using Concertable.B2B.Tenant.Application.DTOs;
using Concertable.B2B.Tenant.Application.Requests;
using Concertable.B2B.Tenant.Contracts;

namespace Concertable.B2B.Tenant.Application.Interfaces;

internal interface ITenantService
{
    Task<TenantDto?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<MembershipDto>> GetMembershipsAsync(Guid userId, CancellationToken ct = default);

    Task<TenantDetails?> GetDetailsForCurrentTenantAsync(CancellationToken ct = default);

    Task<TenantDetails> UpdateAsync(UpdateTenantRequest request, CancellationToken ct = default);
}
