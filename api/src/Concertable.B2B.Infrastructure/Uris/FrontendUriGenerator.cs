using System.Collections.Frozen;
using Microsoft.Extensions.Options;

namespace Concertable.B2B.Infrastructure.Uris;

internal sealed class FrontendUriGenerator : IFrontendUriGenerator
{
    private readonly IUriGenerator uris;
    private readonly FrozenDictionary<FrontendSurface, string> frontends;

    public FrontendUriGenerator(IUriGenerator uris, IOptions<FrontendUrlSettings> settings)
    {
        this.uris = uris;
        this.frontends = settings.Value.Frontends.ToFrozenDictionary();
    }

    public Uri Create(FrontendSurface surface, string path, IDictionary<string, string>? query = null) =>
        uris.Create(frontends[surface], path, query);
}
