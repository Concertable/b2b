using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.Concert.Contracts.Enums;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.DataAccess.Application;

namespace Concertable.B2B.Concert.Infrastructure.Data;

internal static class ConcertQueryExtensions
{
    extension(IQueryable<ConcertEntity> concerts)
    {
        public IQueryable<ConcertEntity> AccessibleBy(
            MembershipSnapshot actor, TenantPermission permission, ConcertAccessScope? scope, DateTime at)
        {
            var binding = permission.BindingFor("concert", scope?.ToString());
            if (binding is null || !actor.HasPermission(permission))
                return concerts.Where(_ => false);

            var accessible = binding.Policy switch
            {
                ResourcePolicy.ConcertGrant => concerts,
                ResourcePolicy.VenuePrincipal => concerts.Where(concert => concert.VenueTenantId == actor.TenantId),
                ResourcePolicy.EitherPrincipal or ResourcePolicy.PrincipalAdministration => concerts.Where(concert =>
                    concert.VenueTenantId == actor.TenantId || concert.ArtistTenantId == actor.TenantId),
                _ => concerts.Where(_ => false),
            };
            foreach (var name in binding.RequiresScopes)
            {
                if (!Enum.TryParse<ConcertAccessScope>(name, false, out var required)
                    || !Enum.IsDefined(required))
                    return concerts.Where(_ => false);
                var live = ResourceAccessGrant<ConcertAccessScope>.IsLiveFor<ConcertAccessGrant>(
                    actor, permission, at, required);
                accessible = accessible.Where(concert => concert.AccessGrants.AsQueryable().Any(live));
            }

            return accessible;
        }
    }
}
