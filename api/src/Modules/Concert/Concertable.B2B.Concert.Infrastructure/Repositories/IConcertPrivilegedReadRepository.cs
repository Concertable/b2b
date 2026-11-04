using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.Concert.Application.DTOs;

namespace Concertable.B2B.Concert.Infrastructure.Repositories;

internal interface IConcertPrivilegedReadRepository
{
    Task<ConcertSummary?> GetSummaryByIdAsync(
        int id, ResourcePolicyBinding binding, MembershipSnapshot actor,
        DateTimeOffset now, CancellationToken ct = default);
    Task<ConcertOperations?> GetOperationsByIdAsync(
        int id, ResourcePolicyBinding binding, MembershipSnapshot actor,
        DateTimeOffset now, CancellationToken ct = default);
    Task<ConcertFinance?> GetFinanceByIdAsync(
        int id, ResourcePolicyBinding binding, ResourcePolicyBinding invoiceBinding,
        MembershipSnapshot actor, DateTimeOffset now, CancellationToken ct = default);
    Task<bool> CanDeclareDoorRevenueByIdAsync(
        int id, ResourcePolicyBinding binding, MembershipSnapshot actor,
        DateTimeOffset now, CancellationToken ct = default);
    Task<IReadOnlyList<ManagerConcertCard>> GetUpcomingCardsForVenueTenantIdAsync(
        Guid venueTenantId, ResourcePolicyBinding binding, MembershipSnapshot actor,
        DateTimeOffset now, CancellationToken ct = default);
    Task<IReadOnlyList<ManagerConcertCard>> GetUpcomingCardsForArtistTenantIdAsync(
        Guid artistTenantId, ResourcePolicyBinding binding, MembershipSnapshot actor,
        DateTimeOffset now, CancellationToken ct = default);
    Task<IReadOnlyList<ConcertDraftReference>> GetDraftReferencesForVenueTenantIdAsync(
        Guid venueTenantId, ResourcePolicyBinding binding, MembershipSnapshot actor,
        DateTimeOffset now, CancellationToken ct = default);
    Task<IReadOnlyList<ConcertSummary>> GetUnpostedByArtistIdAsync(
        int artistId, ResourcePolicyBinding binding, MembershipSnapshot actor,
        DateTimeOffset now, CancellationToken ct = default);
    Task<IReadOnlyList<ConcertSummary>> GetUnpostedByVenueIdAsync(
        int venueId, ResourcePolicyBinding binding, MembershipSnapshot actor,
        DateTimeOffset now, CancellationToken ct = default);
}
