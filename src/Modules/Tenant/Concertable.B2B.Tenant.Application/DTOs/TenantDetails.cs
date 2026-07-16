namespace Concertable.B2B.Tenant.Application.DTOs;

public sealed record TenantDetails
{
    public required Guid Id { get; init; }
    public required string LegalName { get; init; }
    public ComplianceDto? Compliance { get; init; }
    public required Dac7Dto Dac7 { get; init; }
}
