using Concertable.B2B.Tenant.Contracts;
using Microsoft.AspNetCore.Http;

namespace Concertable.B2B.Tenant.Infrastructure.Authorization;

/// <summary>
/// The persona the current request's endpoint defaults its permissions to — read from a controller/action
/// <see cref="TenantPersonaAttribute"/>, or <see langword="null"/> when none is declared. Keeps the routing
/// lookup out of <see cref="PermissionAuthorizationHandler"/>, which inherits it via this seam.
/// </summary>
internal interface IEndpointPersona
{
    TenantType? Value { get; }
}

internal sealed class EndpointPersona : IEndpointPersona
{
    private readonly IHttpContextAccessor httpContextAccessor;

    public EndpointPersona(IHttpContextAccessor httpContextAccessor)
    {
        this.httpContextAccessor = httpContextAccessor;
    }

    public TenantType? Value
        => httpContextAccessor.HttpContext?.GetEndpoint()?.Metadata.GetMetadata<TenantPersonaAttribute>()?.Persona;
}
