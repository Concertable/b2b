using Concertable.Conversations.Contracts;
using Concertable.Kernel;

namespace Concertable.Conversations.Domain;

public class MessageEntity : IIdEntity
{
    private MessageEntity() { }

    public int Id { get; private set; }
    public string Content { get; private set; } = null!;
    public Guid FromUserId { get; private set; }
    public Guid ToUserId { get; private set; }
    public MessageAction? Action { get; private set; }
    public DateTime SentDate { get; private set; }
    public bool Read { get; private set; }

    public static MessageEntity Create(Guid fromUserId, Guid toUserId, string content, DateTime sentDate, MessageAction? action = null) => new()
    {
        FromUserId = fromUserId,
        ToUserId = toUserId,
        Content = content,
        SentDate = sentDate,
        Action = action
    };

    public void MarkAsRead() => Read = true;
}
