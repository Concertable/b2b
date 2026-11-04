namespace Concertable.B2B.Authorization.Contracts;

public interface IResourceAuthorization
{
    Task<AuthorizationDecision> CheckAsync(AuthorizationRequest request, CancellationToken ct = default);

    Task<AuthorizationDecision> RequireAsync(AuthorizationRequest request, CancellationToken ct = default);
}
