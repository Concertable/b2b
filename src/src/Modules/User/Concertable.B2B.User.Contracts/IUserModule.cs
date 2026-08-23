using Reunion;

namespace Concertable.B2B.User.Contracts;

public interface IUserModule
{
    Task<Option<UserDto>> GetByIdAsync(Guid id);
    Task<IReadOnlyList<UserDto>> GetByIdsAsync(IEnumerable<Guid> ids);

    /// <summary>Emails for the given user ids, keyed by id, for member-list display (D4) — the batch join
    /// that keeps email in the User projection instead of denormalizing it onto membership. Ids with no
    /// matching user are absent from the result rather than defaulted.</summary>
    Task<IReadOnlyDictionary<Guid, string>> GetEmailsByIdsAsync(IEnumerable<Guid> ids);

    Task<Option<ManagerDto>> GetManagerByIdAsync(Guid userId);

    /// <summary>Resolves a user id from an email, for callers that only need a yes/no identity check
    /// (e.g. "is this candidate already a user?") without fetching the full user roster.</summary>
    Task<Option<Guid>> GetIdByEmailAsync(string email);
}
