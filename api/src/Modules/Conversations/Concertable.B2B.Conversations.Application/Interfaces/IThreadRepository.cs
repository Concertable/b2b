using Concertable.DataAccess.Application;

namespace Concertable.B2B.Conversations.Application.Interfaces;

internal interface IThreadRepository : IRepository<ThreadEntity>
{
    /// <summary>The thread whose live participants are exactly these tenants, or null if none exists.</summary>
    Task<ThreadEntity?> GetByParticipantsAsync(
        IReadOnlyCollection<Guid> participantTenantIds,
        CancellationToken ct = default);

    /// <summary>The tenants currently able to speak in a thread — the recipients of anything sent into it.</summary>
    Task<IReadOnlyList<Guid>> GetParticipantTenantIdsAsync(int threadId, CancellationToken ct = default);
}
