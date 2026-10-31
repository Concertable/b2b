using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.Conversations.Contracts.Enums;
using Concertable.B2B.DataAccess.Application;

namespace Concertable.B2B.Conversations.Infrastructure.Data;

internal static class ConversationQueryExtensions
{
    extension(IQueryable<ConversationEntity> conversations)
    {
        public IQueryable<ConversationEntity> AccessibleBy(
            MembershipSnapshot actor, TenantPermission permission, ConversationAccessScope? scope, DateTime at)
        {
            var binding = permission.FindBinding(AuthorizationResource.Conversation, scope?.ToString());
            if (binding is null || !actor.HasPermission(permission))
                return conversations.Where(_ => false);

            if (binding.Policy == ResourcePolicy.PrincipalAdministration)
            {
                var principal = ResourceAccessGrant<ConversationAccessScope>.IsLiveFor<ConversationAccessGrant>(
                    actor, permission, at, ConversationAccessScope.Read);
                return conversations.Where(conversation => conversation.AccessGrants.AsQueryable()
                    .Where(grant => grant.Kind == ResourceGrantKind.Principal && grant.MembershipId == null)
                    .Any(principal));
            }

            if (binding.Policy != ResourcePolicy.ConversationGrant)
                return conversations.Where(_ => false);

            var accessible = conversations;
            foreach (var name in binding.RequiresScopes)
            {
                if (!Enum.TryParse<ConversationAccessScope>(name, false, out var required)
                    || !Enum.IsDefined(required))
                    return conversations.Where(_ => false);
                var live = ResourceAccessGrant<ConversationAccessScope>.IsLiveFor<ConversationAccessGrant>(
                    actor, permission, at, required);
                accessible = accessible.Where(conversation => conversation.AccessGrants.AsQueryable().Any(live));
            }

            return accessible;
        }
    }
}
