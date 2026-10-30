using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.Concert.Application.DTOs;
using Concertable.B2B.Concert.Contracts.Enums;

namespace Concertable.B2B.Concert.Infrastructure.Repositories;

internal interface IConcertPrivilegedReadRepository
{
    Task<ConcertSummary?> GetSummaryByIdAsync(
        int id, TenantPermission permission, MembershipSnapshot actor,
        DateTimeOffset now, CancellationToken ct = default);
    Task<ConcertOperations?> GetOperationsByIdAsync(
        int id, TenantPermission permission, MembershipSnapshot actor,
        DateTimeOffset now, CancellationToken ct = default);
    Task<ConcertFinance?> GetFinanceByIdAsync(
        int id, TenantPermission permission, TenantPermission invoicePermission,
        MembershipSnapshot actor, DateTimeOffset now, CancellationToken ct = default);
    Task<bool> IsAccessibleAsync(
        int id, TenantPermission permission, ConcertAccessScope scope, MembershipSnapshot actor,
        DateTimeOffset now, CancellationToken ct = default);
    Task<bool> CanDeclareDoorRevenueByIdAsync(
        int id, TenantPermission permission, MembershipSnapshot actor,
        DateTimeOffset now, CancellationToken ct = default);
    Task<IReadOnlyList<ManagerConcertCard>> GetUpcomingCardsForVenueTenantIdAsync(
        Guid venueTenantId, TenantPermission permission, MembershipSnapshot actor,
        DateTimeOffset now, CancellationToken ct = default);
    Task<IReadOnlyList<ManagerConcertCard>> GetUpcomingCardsForArtistTenantIdAsync(
        Guid artistTenantId, TenantPermission permission, MembershipSnapshot actor,
        DateTimeOffset now, CancellationToken ct = default);
    Task<IReadOnlyList<ConcertDraftReference>> GetDraftReferencesForVenueTenantIdAsync(
        Guid venueTenantId, TenantPermission permission, MembershipSnapshot actor,
        DateTimeOffset now, CancellationToken ct = default);
    Task<IReadOnlyList<ConcertSummary>> GetUnpostedByArtistIdAsync(
        int artistId, TenantPermission permission, MembershipSnapshot actor,
        DateTimeOffset now, CancellationToken ct = default);
    Task<IReadOnlyList<ConcertSummary>> GetUnpostedByVenueIdAsync(
        int venueId, TenantPermission permission, MembershipSnapshot actor,
        DateTimeOffset now, CancellationToken ct = default);
}
