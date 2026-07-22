namespace Concertable.B2B.Infrastructure.Uris;

public sealed class UriGenerator : IUriGenerator
{
    public Uri Create(string baseUrl, string path, IDictionary<string, string>? query = null)
    {
        var builder = new UriBuilder(baseUrl) { Path = path };

        if (query?.Count > 0)
            builder.Query = string.Join("&", query.Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value)}"));

        return builder.Uri;
    }
}
