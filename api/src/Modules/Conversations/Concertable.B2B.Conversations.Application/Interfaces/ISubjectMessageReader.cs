namespace Concertable.B2B.Conversations.Application.Interfaces;

/// <summary>Reads the subject's portable messages fragment — the bodies they authored — for a GDPR
/// access/portability export. Read-only over the unfiltered privileged stance.</summary>
internal interface ISubjectMessageReader
{
    Task<IReadOnlyList<SubjectMessageDto>> GetSubjectMessagesAsync(Guid userId, CancellationToken ct = default);
}
