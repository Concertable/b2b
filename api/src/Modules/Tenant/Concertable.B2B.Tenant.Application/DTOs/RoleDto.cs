namespace Concertable.B2B.Tenant.Application.DTOs;

internal sealed record RolePermissionDto(string Permission, string Audience);

internal sealed record RoleDto(
    Guid Id,
    string Name,
    long Version,
    bool IsSystemPreset,
    bool IsProtectedOwner,
    bool IsInvitationAssignable,
    IReadOnlyList<RolePermissionDto> Permissions);

internal sealed record PermissionMetadataDto(
    string Permission,
    string Label,
    string Category,
    IReadOnlyList<ResourceBindingDto> ResourceBindings,
    IReadOnlyList<string> AssignableAudiences,
    bool OwnerOnly);

internal sealed record ResourceBindingDto(
    string Resource,
    string? Facet,
    string Policy,
    IReadOnlyList<string> RequiresScopes);
