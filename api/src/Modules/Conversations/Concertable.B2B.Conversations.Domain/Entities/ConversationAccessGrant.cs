using Concertable.B2B.Conversations.Contracts.Enums;
using Concertable.B2B.DataAccess.Application;

namespace Concertable.B2B.Conversations.Domain.Entities;

public sealed class ConversationAccessGrant : ResourceAccessGrant<ConversationAccessScope>
{
    private ConversationAccessGrant() { }

    internal static ConversationAccessGrant Issue(
        int conversationId,
        Guid tenantId,
        Guid? membershipId,
        ConversationAccessScope scope,
        Guid issuedByTenantId,
        Guid? issuedByUserId,
        ResourceGrantKind kind,
        DateTime at,
        DateTime? validUntil = null)
    {
        var grant = new ConversationAccessGrant();
        grant.Initialize(
            conversationId,
            tenantId,
            membershipId,
            scope,
            issuedByTenantId,
            issuedByUserId,
            kind,
            at,
            validUntil);
        return grant;
    }

    internal void Revoke(DateTime at) => RevokeCore(at);
}
