using Concertable.B2B.Tenant.Contracts.Enums;

namespace Concertable.B2B.Infrastructure.Uris;

public interface IFrontendUriGenerator
{
    Uri Create(TenantType tenantType, string path, IDictionary<string, string>? query = null);
}
