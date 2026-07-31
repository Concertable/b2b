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

    public async Task SendAsync(Guid venueTenantId, Guid artistTenantId, Guid senderTenantId, Guid sentByUserId, string content, MessageAction action, EmailCopy email)
    {
        await conversationsModule.SendAsync(venueTenantId, artistTenantId, senderTenantId, sentByUserId, content, action);
        await SendEmailCopyAsync(email);
    }

    public async Task SendAndNotifyAsync(Guid venueTenantId, Guid artistTenantId, Guid senderTenantId, Guid sentByUserId, string content, MessageAction action, EmailCopy email)
    {
        await conversationsModule.SendAndNotifyAsync(venueTenantId, artistTenantId, senderTenantId, sentByUserId, content, action);
        await SendEmailCopyAsync(email);
    }

    private async Task SendEmailCopyAsync(EmailCopy email)
    {
        foreach (var recipient in email.Recipients)
            await emailTransport.SendEmailAsync(recipient, email.Subject, email.Body);
    }
}
