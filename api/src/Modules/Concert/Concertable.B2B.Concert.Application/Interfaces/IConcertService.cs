using Concertable.B2B.Concert.Application.DTOs;
using Concertable.B2B.Booking.Contracts;
using Concertable.B2B.Concert.Application.Requests;
using Concertable.B2B.Concert.Application.Responses;
using Concertable.B2B.Concert.Application.Errors;

namespace Concertable.B2B.Concert.Application.Interfaces;

internal interface IConcertService
{
    Task CreateAsync(ConfirmedBookingSnapshot booking, CancellationToken ct = default);
    Task<Result<PublishedConcert, ConcertError>> GetPublishedAsync(
        int id,
        CancellationToken ct = default);
    Task<Result<ConcertSummary, ConcertError>> GetSummaryAsync(
        int id,
        CancellationToken ct = default);
    Task<Result<ConcertOperations, ConcertError>> GetOperationsAsync(
        int id,
        CancellationToken ct = default);
    Task<Result<ConcertFinance, ConcertError>> GetFinanceAsync(
        int id,
        CancellationToken ct = default);
    Task<Result<IReadOnlyList<ConcertDraftReference>, ConcertError>> GetDraftsForCurrentVenueAsync(
        CancellationToken ct = default);
    Task<IReadOnlyList<PublishedConcert>> GetUpcomingByVenueIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<PublishedConcert>> GetUpcomingByArtistIdAsync(int id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ManagerConcertCard>, ConcertError>> GetUpcomingForCurrentVenueAsync();
    Task<Result<IReadOnlyList<ManagerConcertCard>, ConcertError>> GetUpcomingForCurrentArtistAsync();
    Task<Result<ConcertUpdateResponse, UpdateConcertError>> UpdateAsync(
        int id,
        UpdateConcertRequest request,
        CancellationToken ct = default);
    Task<UnitResult<PostConcertError>> PostAsync(
        int id,
        UpdateConcertRequest request,
        CancellationToken ct = default);
    Task<UnitResult<DeclareDoorRevenueError>> DeclareDoorRevenueAsync(
        int id,
        decimal doorRevenue,
        CancellationToken ct = default);
    Task<Result<ConcertSummaryShare, ShareConcertSummaryError>> ShareSummaryAsync(
        int id,
        ShareConcertSummaryRequest request,
        CancellationToken ct = default);
    Task<UnitResult<RevokeConcertSummaryShareError>> RevokeSummaryShareAsync(
        int id,
        Guid grantId,
        long expectedAccessVersion,
        CancellationToken ct = default);
    Task<UnitResult<AssignConcertMemberError>> AssignMemberAsync(
        int id,
        AssignConcertMemberRequest request,
        CancellationToken ct = default);
    Task<UnitResult<AssignConcertMemberError>> RemoveMemberAssignmentAsync(
        int id,
        Guid membershipId,
        CancellationToken ct = default);
    Task<UnitResult<CancelConcertError>> CancelAsync(
        int concertId,
        CancellationToken ct = default);
    Task<IReadOnlyList<PublishedConcert>> GetHistoryByArtistIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<PublishedConcert>> GetHistoryByVenueIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<ConcertSummary>> GetUnpostedByArtistIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<ConcertSummary>> GetUnpostedByVenueIdAsync(int id, CancellationToken ct = default);
}
