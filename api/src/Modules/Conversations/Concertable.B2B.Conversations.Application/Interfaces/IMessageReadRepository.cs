namespace Concertable.B2B.Conversations.Application.Interfaces;

internal interface IMessageReadRepository
{
    Task<IReadOnlyList<MessageEntity>> ListBySenderUserAsync(Guid userId, CancellationToken ct = default);
}
