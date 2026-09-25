namespace Concertable.B2B.Infrastructure.Uris;

public interface IFrontendUriGenerator
{
    Uri Create(FrontendSurface surface, string path, IDictionary<string, string>? query = null);
}
