using Concertable.B2B.Application.Contracts.Enums;
using Concertable.B2B.Application.Domain.Entities;
using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.DataAccess.Application;

namespace Concertable.B2B.Application.Infrastructure.Data;

internal static class ApplicationQueryExtensions
{
    extension(IQueryable<ApplicationEntity> applications)
    {
        public IQueryable<ApplicationEntity> AccessibleBy(
            MembershipSnapshot actor, TenantPermission permission, ApplicationAccessScope scope, DateTime at)
        {
            var binding = permission.FindBinding(AuthorizationResource.Application, scope.ToString());
            if (binding is null || !actor.HasPermission(permission))
                return applications.Where(_ => false);

            var accessible = binding.Policy switch
            {
                ResourcePolicy.ApplicationGrant => applications,
                ResourcePolicy.VenuePrincipal => applications.Where(application =>
                    application.VenueTenantId == actor.TenantId),
                ResourcePolicy.ArtistPrincipal => applications.Where(application =>
                    application.ArtistTenantId == actor.TenantId),
                _ => applications.Where(_ => false),
            };
            foreach (var name in binding.RequiresScopes)
            {
                if (!Enum.TryParse<ApplicationAccessScope>(name, false, out var required)
                    || !Enum.IsDefined(required))
                    return applications.Where(_ => false);
                var live = ResourceAccessGrant<ApplicationAccessScope>.IsLiveFor<ApplicationAccessGrant>(
                    actor, permission, at, required);
                accessible = accessible.Where(application => application.AccessGrants.AsQueryable().Any(live));
            }

            return accessible;
        }
    }
}
