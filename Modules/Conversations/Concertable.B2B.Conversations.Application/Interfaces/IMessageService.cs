using Concertable.Contracts;
using Concertable.B2B.Conversations.Application.DTOs;

namespace Concertable.B2B.Conversations.Application.Interfaces;

internal interface IMessageService
{
    Task SendAsync(Guid fromUserId, Guid toUserId, string content, MessageAction? action = null);
    Task SendAndNotifyAsync(Guid fromUserId, Guid toUserId, string content, MessageAction? action = null);
    Task<MessageSummaryDto> GetSummaryForUser();
    Task<IPagination<MessageDto>> GetForUserAsync(IPageParams pageParams);
    Task<int> GetUnreadCountForUserAsync();
    Task MarkAsReadAsync(List<int> ids);
}
