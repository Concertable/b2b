namespace Concertable.B2B.Tenant.Application.DTOs;

internal sealed record PermissionMetadataDto(
    string Permission,
    string Label,
    string Category,
    IReadOnlyList<ResourceBinding> ResourceBindings,
    IReadOnlyList<string> AssignableAudiences,
    bool OwnerOnly);
