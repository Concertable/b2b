using Concertable.B2B.Concert.Application.DTOs;
using Concertable.B2B.Concert.Application.Requests;
using Concertable.B2B.Concert.Application.Responses;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Application.Errors;

namespace Concertable.B2B.Concert.Application.Interfaces;

internal interface IConcertService
{
    Task<Result<ConcertDetails, ConcertError>> GetDetailsByIdAsync(int id);
    Task<Result<ConcertDetails, ConcertError>> GetDetailsForCurrentUserAsync(int id);
    Task<Result<ConcertDetails, ConcertError>> GetDetailsByApplicationIdAsync(int applicationId);
    Task<IReadOnlyList<ConcertSummary>> GetUpcomingByVenueIdAsync(int id);
    Task<IReadOnlyList<ConcertSummary>> GetUpcomingByArtistIdAsync(int id);
    Task<Result<ConcertEntity, CreateConcertDraftError>> CreateDraftAsync(int applicationId);
    Task<Result<ConcertUpdateResponse, UpdateConcertError>> UpdateAsync(int id, UpdateConcertRequest request);
    Task<UnitResult<PostConcertError>> PostAsync(int id, UpdateConcertRequest request);
    Task<UnitResult<DeclareDoorRevenueError>> DeclareDoorRevenueAsync(int id, decimal doorRevenue);
    Task<IReadOnlyList<ConcertSummary>> GetHistoryByArtistIdAsync(int id);
    Task<IReadOnlyList<ConcertSummary>> GetHistoryByVenueIdAsync(int id);
    Task<IReadOnlyList<ConcertSummary>> GetUnpostedByArtistIdAsync(int id);
    Task<IReadOnlyList<ConcertSummary>> GetUnpostedByVenueIdAsync(int id);
}
