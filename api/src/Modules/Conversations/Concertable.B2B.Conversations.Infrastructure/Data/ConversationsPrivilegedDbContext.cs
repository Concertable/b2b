using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.Messaging.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Concertable.B2B.Conversations.Infrastructure.Data;

internal sealed class ConversationsPrivilegedDbContext(
    DbContextOptions<ConversationsPrivilegedDbContext> options,
    IOptions<OutboxOptions> outboxOptions,
    ConversationsConfigurationProvider provider)
    : PrivilegedDbContext(options, outboxOptions, provider, Schema.Name)
{
    public DbSet<ContentReportEntity> ContentReports => Set<ContentReportEntity>();
    public DbSet<ConversationAccessGrant> ConversationAccessGrants => Set<ConversationAccessGrant>();
    public DbSet<ConversationCreationReceipt> ConversationCreationReceipts => Set<ConversationCreationReceipt>();
    public DbSet<ConversationReadPosition> ConversationReadPositions => Set<ConversationReadPosition>();
    public DbSet<MessageEntity> Messages => Set<MessageEntity>();
    public DbSet<ConversationEntity> Conversations => Set<ConversationEntity>();
    public DbSet<TenantDisplay> TenantDisplays => Set<TenantDisplay>();
}
