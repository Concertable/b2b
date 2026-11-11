using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Tenant.Infrastructure.Extensions;

internal static class DbUpdateExceptionExtensions
{
    extension(DbUpdateException exception)
    {
        public bool IsMembershipConcurrencyConflict() =>
            exception is DbUpdateConcurrencyException
            && exception.Entries.Any(entry => entry.Entity is TenantMembershipEntity or MembershipRoleAssignment);
    }
}
