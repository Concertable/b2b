using Concertable.Contracts;

namespace Concertable.B2B.Conversations.Application.Interfaces;

internal interface IMessageRepository
{
    Task<IPagination<MessageEntity>> GetByUserIdAsync(Guid id, IPageParams pageParams);
    Task<int> GetUnreadCountByUserIdAsync(Guid id);
    Task MarkAsReadAsync(List<int> ids);
    Task AddAsync(MessageEntity message);
    Task SaveChangesAsync();
}
