using System.Collections.Frozen;
using Concertable.B2B.Tenant.Contracts.Enums;
using Microsoft.Extensions.Options;

namespace Concertable.B2B.Infrastructure.Uris;

internal sealed class FrontendUriGenerator : IFrontendUriGenerator
{
    private readonly IUriGenerator uris;
    private readonly FrozenDictionary<TenantType, string> frontends;

    public FrontendUriGenerator(IUriGenerator uris, IOptions<FrontendUrlSettings> settings)
    {
        this.uris = uris;
        this.frontends = settings.Value.Frontends.ToFrozenDictionary();
    }

    public Uri Create(TenantType persona, string path, IDictionary<string, string>? query = null) =>
        uris.Create(frontends[persona], path, query);
}
