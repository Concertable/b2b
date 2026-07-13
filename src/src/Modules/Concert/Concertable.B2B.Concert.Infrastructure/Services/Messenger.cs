using Concertable.B2B.Conversations.Contracts;
using Concertable.Shared.Email.Application;

namespace Concertable.B2B.Concert.Infrastructure.Services;

internal sealed class Messenger : IMessenger
{
    private readonly IConversationsModule conversationsModule;
    private readonly IEmailSender emailSender;

    public Messenger(IConversationsModule conversationsModule, IEmailSender emailSender)
    {
        this.conversationsModule = conversationsModule;
        this.emailSender = emailSender;
    }

    public async Task SendAsync(Guid fromUserId, Guid toUserId, string content, MessageAction action, EmailCopy email)
    {
        await conversationsModule.SendAsync(fromUserId, toUserId, content, action);
        await emailSender.SendEmailAsync(email.To, email.Subject, email.Body);
    }

    public async Task SendAndNotifyAsync(Guid fromUserId, Guid toUserId, string content, MessageAction action, EmailCopy email)
    {
        await conversationsModule.SendAndNotifyAsync(fromUserId, toUserId, content, action);
        await emailSender.SendEmailAsync(email.To, email.Subject, email.Body);
    }
}
