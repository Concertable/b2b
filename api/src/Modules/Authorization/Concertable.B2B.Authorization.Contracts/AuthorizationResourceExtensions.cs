using System.Text.Json;

namespace Concertable.B2B.Authorization.Contracts;

public static class AuthorizationResourceExtensions
{
    extension(AuthorizationResource resource)
    {
        public string WireName => JsonNamingPolicy.SnakeCaseLower.ConvertName(resource.ToString());
    }
}
