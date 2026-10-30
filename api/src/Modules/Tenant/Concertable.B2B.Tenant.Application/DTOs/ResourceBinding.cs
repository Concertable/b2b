namespace Concertable.B2B.Tenant.Application.DTOs;

internal sealed record ResourceBinding(
    string Resource,
    string? Facet,
    string Policy,
    IReadOnlyList<string> RequiresScopes);
