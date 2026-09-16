using Concertable.B2B.Application.Contracts.Enums;

namespace Concertable.B2B.Application.Application.DTOs;

internal sealed record ShareApplicationRequest
{
    public Guid ToTenantId { get; init; }
    public Guid? ToMemberUserId { get; init; }
    public ApplicationAccessScope Scope { get; init; }
    public DateTime? ValidUntil { get; init; }
}

internal sealed record ApplicationShareResponse
{
    public Guid GrantId { get; init; }
    public Guid ToTenantId { get; init; }
    public Guid? ToMemberUserId { get; init; }
    public ApplicationAccessScope Scope { get; init; }
    public DateTime ValidFrom { get; init; }
    public DateTime? ValidUntil { get; init; }
}
