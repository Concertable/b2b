namespace Concertable.B2B.Infrastructure.Uris;

public interface IUriGenerator
{
    Uri Create(string baseUrl, string path, IDictionary<string, string>? query = null);
}
