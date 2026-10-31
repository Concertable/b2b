using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.Booking.Contracts.Enums;
using Concertable.B2B.Booking.Domain.Entities;
using Concertable.B2B.DataAccess.Application;

namespace Concertable.B2B.Booking.Infrastructure.Data;

internal static class ContractQueryExtensions
{
    extension(IQueryable<ContractEntity> contracts)
    {
        public IQueryable<ContractEntity> AccessibleBy(
            MembershipSnapshot actor, TenantPermission permission, ContractAccessScope scope, DateTime at)
        {
            var binding = permission.FindBinding(AuthorizationResource.Contract, scope.ToString());
            if (binding is null || !actor.HasPermission(permission) || binding.Policy != ResourcePolicy.ContractGrant)
                return contracts.Where(_ => false);

            var accessible = contracts;
            foreach (var name in binding.RequiresScopes)
            {
                if (!Enum.TryParse<ContractAccessScope>(name, false, out var required)
                    || !Enum.IsDefined(required))
                    return contracts.Where(_ => false);
                var live = ResourceAccessGrant<ContractAccessScope>.IsLiveFor<ContractAccessGrant>(
                    actor, permission, at, required);
                accessible = accessible.Where(contract => contract.AccessGrants.AsQueryable().Any(live));
            }

            return accessible;
        }
    }
}
