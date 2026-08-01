using Concertable.Kernel.Functional;

namespace Concertable.B2B.User.Contracts;

public interface IUserModule
{
    Task<Option<UserBase>> GetByIdAsync(Guid id);
    Task<IReadOnlyList<UserBase>> GetByIdsAsync(IEnumerable<Guid> ids);

    /// <summary>Emails for the given user ids, keyed by id, for member-list display (D4) — the batch join
    /// that keeps email in the User projection instead of denormalizing it onto membership. Ids with no
    /// matching user are absent from the result rather than defaulted.</summary>
    Task<IReadOnlyDictionary<Guid, string>> GetEmailsByIdsAsync(IEnumerable<Guid> ids);

    Task<Option<ManagerDto>> GetManagerByIdAsync(Guid userId);
}
