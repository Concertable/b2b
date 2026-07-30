using Concertable.B2B.Conversations.Contracts;
using Concertable.Shared.Email.Application;

namespace Concertable.B2B.Concert.Infrastructure.Services;

internal sealed class Messenger : IMessenger
{
    private readonly IConversationsModule conversationsModule;
    private readonly IEmailTransport emailTransport;

    public Messenger(IConversationsModule conversationsModule, IEmailTransport emailTransport)
    {
        this.conversationsModule = conversationsModule;
        this.emailTransport = emailTransport;
    }

    public async Task SendAsync(Guid fromUserId, Guid toUserId, string content, MessageAction action, EmailCopy email)
    {
        await conversationsModule.SendAsync(fromUserId, toUserId, content, action);
        await emailTransport.SendEmailAsync(email.To, email.Subject, email.Body);
    }

    public async Task SendAndNotifyAsync(Guid fromUserId, Guid toUserId, string content, MessageAction action, EmailCopy email)
    {
        await conversationsModule.SendAndNotifyAsync(fromUserId, toUserId, content, action);
        await emailTransport.SendEmailAsync(email.To, email.Subject, email.Body);
    }
}
