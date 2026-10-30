using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.Booking.Contracts.Enums;
using Concertable.B2B.Booking.Domain.Entities;
using Concertable.B2B.DataAccess.Application;

namespace Concertable.B2B.Booking.Infrastructure.Data;

internal static class BookingQueryExtensions
{
    extension(IQueryable<BookingEntity> bookings)
    {
        public IQueryable<BookingEntity> AccessibleBy(
            MembershipSnapshot actor, TenantPermission permission, BookingAccessScope scope, DateTime at)
        {
            var binding = permission.BindingFor("booking", scope.ToString());
            if (binding is null || !actor.HasPermission(permission))
                return bookings.Where(_ => false);

            var accessible = binding.Policy switch
            {
                ResourcePolicy.BookingGrant => bookings,
                ResourcePolicy.EitherPrincipal => bookings.Where(booking =>
                    booking.VenueTenantId == actor.TenantId || booking.ArtistTenantId == actor.TenantId),
                _ => bookings.Where(_ => false),
            };
            foreach (var name in binding.RequiresScopes)
            {
                if (!Enum.TryParse<BookingAccessScope>(name, false, out var required)
                    || !Enum.IsDefined(required))
                    return bookings.Where(_ => false);
                var live = ResourceAccessGrant<BookingAccessScope>.IsLiveFor<BookingAccessGrant>(
                    actor, permission, at, required);
                accessible = accessible.Where(booking => booking.AccessGrants.AsQueryable().Any(live));
            }

            return accessible;
        }
    }
}
