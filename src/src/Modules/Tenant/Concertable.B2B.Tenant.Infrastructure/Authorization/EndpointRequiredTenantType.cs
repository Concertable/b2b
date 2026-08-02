using Concertable.B2B.Tenant.Contracts;
using Microsoft.AspNetCore.Http;

namespace Concertable.B2B.Tenant.Infrastructure.Authorization;

/// <summary>Provides the tenant type required by the current endpoint.</summary>
internal interface IEndpointRequiredTenantType
{
    TenantType? RequiredTenantType { get; }
}

internal sealed class EndpointRequiredTenantType : IEndpointRequiredTenantType
{
    private readonly IHttpContextAccessor httpContextAccessor;

    public EndpointRequiredTenantType(IHttpContextAccessor httpContextAccessor)
    {
        this.httpContextAccessor = httpContextAccessor;
    }

    public TenantType? RequiredTenantType
        => httpContextAccessor.HttpContext?.GetEndpoint()?.Metadata.GetMetadata<RequiredTenantTypeAttribute>()?.TenantType;
}
