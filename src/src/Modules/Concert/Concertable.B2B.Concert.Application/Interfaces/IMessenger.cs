using Concertable.B2B.Conversations.Contracts;

namespace Concertable.B2B.Concert.Application.Interfaces;

/// <summary>An email copy of a conversation message, fanned out to every recipient-tenant member.</summary>
internal sealed record EmailCopy(IReadOnlyList<string> Recipients, string Subject, string Body);

/// <summary>Sends a tenant-owned conversation message with an email copy to the recipient tenant's members.
/// Verbs mirror <see cref="IConversationsModule"/>: SendAndNotify also raises an in-app notification.</summary>
internal interface IMessenger
{
    Task SendAsync(Guid venueTenantId, Guid artistTenantId, Guid senderTenantId, Guid sentByUserId, string content, MessageAction action, EmailCopy email);
    Task SendAndNotifyAsync(Guid venueTenantId, Guid artistTenantId, Guid senderTenantId, Guid sentByUserId, string content, MessageAction action, EmailCopy email);
}
