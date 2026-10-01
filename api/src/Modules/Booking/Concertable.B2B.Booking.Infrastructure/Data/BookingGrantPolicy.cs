using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.Booking.Contracts.Enums;
using Concertable.B2B.Booking.Domain.Entities;

namespace Concertable.B2B.Booking.Infrastructure.Data;

internal static class BookingGrantPolicy
{
    public static IQueryable<BookingEntity> EligibleBookingResources(
        IQueryable<BookingEntity> bookings,
        Guid tenantId,
        string policy) => policy switch
    {
        "booking_grant" => bookings,
        "either_principal" => bookings.Where(booking =>
            booking.VenueTenantId == tenantId || booking.ArtistTenantId == tenantId),
        _ => bookings.Where(_ => false),
    };

    public static IQueryable<ContractEntity> EligibleContractResources(
        IQueryable<ContractEntity> contracts,
        string policy) => policy == "contract_grant"
        ? contracts
        : contracts.Where(_ => false);
    public static IQueryable<BookingAccessGrant> EligibleBookings(
        IQueryable<BookingAccessGrant> grants,
        MembershipSnapshot actor,
        TenantPermission permission,
        BookingAccessScope scope,
        DateTime now)
    {
        var audience = actor.AudienceFor(permission);
        return grants.Where(grant =>
            grant.TenantId == actor.TenantId
            && grant.Scope == scope
            && grant.RevokedAt == null
            && grant.ValidFrom <= now
            && (grant.ValidUntil == null || now < grant.ValidUntil)
            && (audience == ResourceAudience.TenantResources
                    && (grant.MembershipId == null || grant.MembershipId == actor.MembershipId)
                || audience == ResourceAudience.AssignedResources
                    && grant.MembershipId == actor.MembershipId));
    }

    public static IQueryable<ContractAccessGrant> EligibleContracts(
        IQueryable<ContractAccessGrant> grants,
        MembershipSnapshot actor,
        TenantPermission permission,
        ContractAccessScope scope,
        DateTime now)
    {
        var audience = actor.AudienceFor(permission);
        return grants.Where(grant =>
            grant.TenantId == actor.TenantId
            && grant.Scope == scope
            && grant.RevokedAt == null
            && grant.ValidFrom <= now
            && (grant.ValidUntil == null || now < grant.ValidUntil)
            && (audience == ResourceAudience.TenantResources
                    && (grant.MembershipId == null || grant.MembershipId == actor.MembershipId)
                || audience == ResourceAudience.AssignedResources
                    && grant.MembershipId == actor.MembershipId));
    }
}