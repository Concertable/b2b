using Concertable.B2B.Conversations.Contracts.Enums;
using Concertable.B2B.DataAccess.Infrastructure;

namespace Concertable.B2B.Conversations.Infrastructure.Data.Configurations;

internal sealed class ThreadAccessGrantConfiguration
    : ResourceAccessGrantConfiguration<ThreadAccessGrant, ThreadAccessScope>
{
    protected override string TableName => Schema.Tables.ThreadAccessGrants;

    protected override string SchemaName => Schema.Name;
}
