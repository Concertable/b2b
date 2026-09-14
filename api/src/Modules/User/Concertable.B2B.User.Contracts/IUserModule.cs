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

    /// <summary>GDPR erasure (art. 17): anonymises this subject's B2B User row in place — email tombstoned to a
    /// stable per-subject pseudonym, location/address/avatar dropped — while keeping the row and its id (the Auth
    /// <c>sub</c>) so downstream foreign keys stay valid. Idempotent: a no-op when the subject has no B2B User row.</summary>
    Task EraseAsync(Guid subjectId, CancellationToken ct = default);

    /// <summary>The subject's portable User fragment (GDPR arts. 15/20), or None when they have no B2B User row.</summary>
    Task<Option<UserExport>> ExportUserAsync(Guid subjectId, CancellationToken ct = default);
}
