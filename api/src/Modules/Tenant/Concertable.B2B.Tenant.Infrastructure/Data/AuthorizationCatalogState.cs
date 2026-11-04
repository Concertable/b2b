namespace Concertable.B2B.Tenant.Infrastructure.Data;

internal sealed class AuthorizationCatalogState
{
    public int Id { get; private set; }
    public string Revision { get; private set; } = null!;
}
