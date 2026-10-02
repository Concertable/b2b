namespace Concertable.B2B.Conversations.Application.Interfaces;

internal interface ISubjectMessageReader
{
    Task<IReadOnlyList<SubjectMessageDto>> GetSubjectMessagesAsync(Guid userId, CancellationToken ct = default);
}
