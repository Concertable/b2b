using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.Booking.Contracts.Enums;
using Concertable.B2B.Booking.Domain.Entities;
using Concertable.B2B.DataAccess.Application;

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
    public static IQueryable<BookingEntity> VisibleBookings(
        IQueryable<BookingEntity> bookings, IQueryable<BookingAccessGrant> grants,
        IQueryable<MembershipAuthority> members, MembershipSnapshot actor,
        ResourcePolicyBinding binding, DateTime now)
    {
        var visible = EligibleBookingResources(bookings, actor.TenantId, binding.Policy)
            .Where(booking => members.Any(member =>
                member.MembershipId == actor.MembershipId
                && member.TenantId == actor.TenantId
                && member.UserId == actor.UserId
                && member.PermissionVersion == actor.PermissionVersion
                && member.RolePolicyVersion == actor.RolePolicyVersion));
        foreach (var name in binding.RequiredScopes)
        {
            if (!Enum.TryParse<BookingAccessScope>(name, false, out var scope)
                || !Enum.IsDefined(scope))
                throw new InvalidOperationException("An invalid booking scope was registered.");
            var eligible = EligibleBookings(grants, actor, binding.Permission, scope, now);
            visible = visible.Where(booking => eligible.Any(grant => grant.ResourceId == booking.Id));
        }
        return visible;
    }

    public static IQueryable<ContractEntity> VisibleContracts(
        IQueryable<ContractEntity> contracts, IQueryable<ContractAccessGrant> grants,
        IQueryable<MembershipAuthority> members, MembershipSnapshot actor,
        ResourcePolicyBinding binding, DateTime now)
    {
        var visible = EligibleContractResources(contracts, binding.Policy)
            .Where(contract => members.Any(member =>
                member.MembershipId == actor.MembershipId
                && member.TenantId == actor.TenantId
                && member.UserId == actor.UserId
                && member.PermissionVersion == actor.PermissionVersion
                && member.RolePolicyVersion == actor.RolePolicyVersion));
        foreach (var name in binding.RequiredScopes)
        {
            if (!Enum.TryParse<ContractAccessScope>(name, false, out var scope)
                || !Enum.IsDefined(scope))
                throw new InvalidOperationException("An invalid contract scope was registered.");
            var eligible = EligibleContracts(grants, actor, binding.Permission, scope, now);
            visible = visible.Where(contract => eligible.Any(grant => grant.ResourceId == contract.Id));
        }
        return visible;
    }

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