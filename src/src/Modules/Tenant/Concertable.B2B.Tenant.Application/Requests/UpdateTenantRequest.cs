using Concertable.B2B.Tenant.Application.DTOs;

namespace Concertable.B2B.Tenant.Application.Requests;

internal sealed record UpdateTenantRequest
{
    public required string LegalName { get; init; }
    public required ComplianceDto Compliance { get; init; }
}
