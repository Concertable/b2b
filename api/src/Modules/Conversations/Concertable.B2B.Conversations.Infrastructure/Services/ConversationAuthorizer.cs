using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.Conversations.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Conversations.Infrastructure.Services;

internal interface IConversationAuthorizer
{
    Task<UnitResult<AccessDenied>> AuthorizeAsync(
        int id, TenantPermission permission, ConversationAccessScope? scope, CancellationToken ct = default);
}

internal sealed class ConversationAuthorizer(
    ConversationsPrivilegedDbContext context,
    UnitOfWorkAccessor unitOfWorkAccessor,
    IMembershipContext membership,
    IMembershipReadRepository memberships,
    TimeProvider clock) : IConversationAuthorizer
{
    public async Task<UnitResult<AccessDenied>> AuthorizeAsync(
        int id, TenantPermission permission, ConversationAccessScope? scope, CancellationToken ct = default)
    {
        if (membership.Membership is not { } expected
            || await memberships.LockIfUnchangedAsync(expected, ct) is not { } actor
            || !actor.HasPermission(permission))
            return new AccessDenied();

        var unitOfWork = unitOfWorkAccessor.Current
            ?? throw new InvalidOperationException("A conversation authorization requires an active unit of work.");
        await unitOfWork.EnlistAsync(context, ct);
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            SELECT 1 FROM conversations."Conversations"
            WHERE "Id" = {id}
            FOR UPDATE
            """, ct);
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            SELECT 1 FROM conversations."ConversationAccessGrants"
            WHERE "ResourceId" = {id}
            ORDER BY "Id"
            FOR UPDATE
            """, ct);

        return await context.Conversations.AsNoTracking()
            .AccessibleBy(actor, permission, scope, clock.GetUtcNow().UtcDateTime)
            .AnyAsync(conversation => conversation.Id == id, ct)
            ? UnitResult.Success<AccessDenied>()
            : new AccessDenied();
    }
}
