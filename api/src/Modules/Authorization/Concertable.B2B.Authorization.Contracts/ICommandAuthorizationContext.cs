namespace Concertable.B2B.Authorization.Contracts;

public interface ICommandAuthorizationContext
{
    bool IsActive { get; }

    Guid? TransactionId { get; }

    void RegisterFailure<TResult>(Func<TResult> authorityFailure);

    void RegisterValidator(Func<CancellationToken, Task<bool>> validator);

    void MarkAuthorityFailed();
}
