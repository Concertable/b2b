namespace Concertable.B2B.Tenant.Infrastructure.Data;

internal sealed class AuthorizationCatalogState
{
    private const int SingletonId = 1;

    private AuthorizationCatalogState() { }

    public int Id { get; private set; }
    public string Revision { get; private set; } = null!;

    public static AuthorizationCatalogState Create(string revision) => new() { Id = SingletonId, Revision = revision };

    public void Install(string revision) => Revision = revision;
}
