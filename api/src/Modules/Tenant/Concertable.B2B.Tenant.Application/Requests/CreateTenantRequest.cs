namespace Concertable.B2B.Tenant.Application.Requests;

internal sealed record CreateTenantRequest(
    string DisplayName,
    string ContactEmail,
    IReadOnlyList<TenantBusinessActivityKind> Activities);
