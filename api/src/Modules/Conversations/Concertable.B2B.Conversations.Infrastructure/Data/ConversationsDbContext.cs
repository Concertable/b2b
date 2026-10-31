using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.Conversations.Domain.ReadModels;
using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.Kernel.Identity;
using Concertable.Messaging.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Concertable.B2B.Conversations.Infrastructure.Data;

internal sealed class ConversationsDbContext(
    DbContextOptions<ConversationsDbContext> options,
    IOptions<OutboxOptions> outboxOptions,
    ConversationsConfigurationProvider provider,
    ITenantContext tenantContext,
    IAuthorizationContext authorization,
    TimeProvider timeProvider)
    : ResourceScopedDbContext(options, outboxOptions, provider, tenantContext, authorization, timeProvider, Schema.Name)
{
    public DbSet<ContentReportEntity> ContentReports => Set<ContentReportEntity>();
    public DbSet<MessageEntity> Messages => Set<MessageEntity>();
    public DbSet<ConversationEntity> Conversations => Set<ConversationEntity>();
    public DbSet<ConversationReadPosition> ConversationReadPositions => Set<ConversationReadPosition>();
    public DbSet<TenantDisplay> TenantDisplays => Set<TenantDisplay>();

    public ResourceAccess ReadAccess => ActorAccess(TenantPermission.MessagesRead);

    protected override void ApplyTenantFilters(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ConversationEntity>().HasQueryFilter(TenantFilters.Key, conversation =>
            ReadAccess == ResourceAccess.All
                && conversation.Participants.Any(participant => participant.TenantId == ActorTenantId)
            || ReadAccess != ResourceAccess.None
                && conversation.Assignments.Any(assignment => assignment.MembershipId == ActorMembershipId));

        modelBuilder.Entity<MessageEntity>().HasQueryFilter(TenantFilters.Key, message =>
            Conversations.Any(conversation => conversation.Id == message.ConversationId));

        modelBuilder.Entity<ConversationReadPosition>().HasQueryFilter(TenantFilters.Key, state =>
            state.TenantId == ActorTenantId
            && state.MembershipId == ActorMembershipId
            && Conversations.Any(conversation => conversation.Id == state.ConversationId));

        modelBuilder.Entity<ContentReportEntity>().HasQueryFilter(TenantFilters.Key, report =>
            report.ReporterTenantId == ActorTenantId
            && report.ReporterUserId == ActorUserId
            && Conversations.Any(conversation => conversation.Id == report.ConversationId));
    }
}
