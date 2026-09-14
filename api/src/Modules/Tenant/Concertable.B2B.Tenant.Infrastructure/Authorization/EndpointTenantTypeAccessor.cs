using Concertable.B2B.Tenant.Contracts;
using Microsoft.AspNetCore.Http;

namespace Concertable.B2B.Tenant.Infrastructure.Authorization;

internal interface IEndpointTenantTypeAccessor
{
    TenantType? TenantType { get; }
}

internal sealed class EndpointTenantTypeAccessor : IEndpointTenantTypeAccessor
{
    private readonly IHttpContextAccessor httpContextAccessor;

    public EndpointTenantTypeAccessor(IHttpContextAccessor httpContextAccessor)
    {
        this.httpContextAccessor = httpContextAccessor;
    }

    public TenantType? TenantType
        => httpContextAccessor.HttpContext?.GetEndpoint()?.Metadata.GetMetadata<RequiredTenantTypeAttribute>()?.TenantType;
}
