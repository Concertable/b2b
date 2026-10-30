using Concertable.B2B.Authorization.Contracts;

namespace Concertable.B2B.DataAccess.Infrastructure;

internal sealed class AuthorizationContext(UnitOfWorkAccessor accessor) : IAuthorizationContext
{
    public bool IsActive => accessor.Current is not null;
    public Guid? UnitOfWorkId => accessor.Current?.Id;

    public void RegisterFailure<TResult>(Func<TResult> authorityFailure) =>
        Current.RegisterAuthorityFailure(authorityFailure);

    public void RegisterValidator(Func<CancellationToken, Task<bool>> validator) =>
        Current.RegisterRequiredAuthority(validator);

    public void MarkAuthorityFailed() => Current.MarkAuthorityFailed();

    private UnitOfWork Current => accessor.Current
        ?? throw new InvalidOperationException("No unit of work is active.");
}
