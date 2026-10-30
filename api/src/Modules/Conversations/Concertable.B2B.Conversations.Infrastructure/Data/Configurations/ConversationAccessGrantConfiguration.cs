using Concertable.B2B.Conversations.Contracts.Enums;
using Concertable.B2B.DataAccess.Infrastructure;

namespace Concertable.B2B.Conversations.Infrastructure.Data.Configurations;

internal sealed class ConversationAccessGrantConfiguration
    : ResourceAccessGrantConfiguration<ConversationAccessGrant, ConversationAccessScope>
{
    protected override string TableName => Schema.Tables.ConversationAccessGrants;

    protected override string SchemaName => Schema.Name;
}
