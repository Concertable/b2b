using Concertable.Contracts;
using Concertable.B2B.Conversations.Application.DTOs;

namespace Concertable.B2B.Conversations.Application.Interfaces;

internal interface IMessageService
{
    Task SendAsync(Guid venueTenantId, Guid artistTenantId, Guid senderTenantId, Guid sentByUserId, string content, MessageAction? action = null);
    Task SendAndNotifyAsync(Guid venueTenantId, Guid artistTenantId, Guid senderTenantId, Guid sentByUserId, string content, MessageAction? action = null);
    Task<MessageSummary> GetInboxSummaryAsync();
    Task<IPagination<MessageDto>> GetInboxAsync(IPageParams pageParams);
    Task<int> GetUnreadCountForUserAsync();
    Task MarkAsReadAsync(Guid counterpartTenantId);
}
