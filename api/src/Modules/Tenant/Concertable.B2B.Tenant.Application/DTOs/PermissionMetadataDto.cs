namespace Concertable.B2B.Tenant.Application.DTOs;

internal sealed record PermissionMetadataDto(
    string Permission,
    string Label,
    string Category,
    bool OwnerOnly);
