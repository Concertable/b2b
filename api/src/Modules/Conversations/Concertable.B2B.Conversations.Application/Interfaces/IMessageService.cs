using Concertable.Contracts;
using Concertable.B2B.Conversations.Application.DTOs;

namespace Concertable.B2B.Conversations.Application.Interfaces;

internal interface IMessageService
{
    Task SendAsync(
        IReadOnlyCollection<Guid> participantTenantIds,
        Guid senderTenantId,
        Guid sentByUserId,
        string content,
        MessageAction? action = null);

    Task SendAndNotifyAsync(
        IReadOnlyCollection<Guid> participantTenantIds,
        Guid senderTenantId,
        Guid sentByUserId,
        string content,
        MessageAction? action = null);

    Task<IPagination<MessageDto>> GetInboxAsync(IPageParams pageParams);
    Task<int> GetUnreadCountForUserAsync();
    Task<IReadOnlyList<MessagePreviewDto>> GetRecentPreviewsAsync();
    Task MarkInboxReadAsync();
}
