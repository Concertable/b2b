using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.Concert.Contracts.Enums;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.DataAccess.Application;

namespace Concertable.B2B.Concert.Infrastructure.Data;

internal static class InvoiceQueryExtensions
{
    extension(IQueryable<InvoiceEntity> invoices)
    {
        public IQueryable<InvoiceEntity> AccessibleBy(
            MembershipSnapshot actor, TenantPermission permission, InvoiceAccessScope scope, DateTime at)
        {
            var binding = permission.BindingFor("invoice", scope.ToString());
            if (binding is null || !actor.HasPermission(permission) || binding.Policy != ResourcePolicy.InvoiceGrant)
                return invoices.Where(_ => false);

            var accessible = invoices;
            foreach (var name in binding.RequiresScopes)
            {
                if (!Enum.TryParse<InvoiceAccessScope>(name, false, out var required)
                    || !Enum.IsDefined(required))
                    return invoices.Where(_ => false);
                var live = ResourceAccessGrant<InvoiceAccessScope>.IsLiveFor<InvoiceAccessGrant>(
                    actor, permission, at, required);
                accessible = accessible.Where(invoice => invoice.AccessGrants.AsQueryable().Any(live));
            }

            return accessible;
        }
    }
}
