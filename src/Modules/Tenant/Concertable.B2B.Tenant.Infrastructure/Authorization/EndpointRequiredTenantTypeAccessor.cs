using Concertable.B2B.Tenant.Contracts;
using Microsoft.AspNetCore.Http;

namespace Concertable.B2B.Tenant.Infrastructure.Authorization;

internal interface IEndpointRequiredTenantTypeAccessor
{
    TenantType? RequiredTenantType { get; }
}

internal sealed class EndpointRequiredTenantTypeAccessor : IEndpointRequiredTenantTypeAccessor
{
    private readonly IHttpContextAccessor httpContextAccessor;

    public EndpointRequiredTenantTypeAccessor(IHttpContextAccessor httpContextAccessor)
    {
        this.httpContextAccessor = httpContextAccessor;
    }

    public TenantType? RequiredTenantType
        => httpContextAccessor.HttpContext?.GetEndpoint()?.Metadata.GetMetadata<RequiredTenantTypeAttribute>()?.TenantType;
}
