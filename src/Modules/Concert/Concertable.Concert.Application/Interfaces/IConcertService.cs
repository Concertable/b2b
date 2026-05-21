using Concertable.Concert.Application.DTOs;
using Concertable.Concert.Application.Requests;
using Concertable.Concert.Application.Responses;
using FluentResults;

namespace Concertable.Concert.Application.Interfaces;

internal interface IConcertService
{
    Task<ConcertDto> GetDetailsByIdAsync(int id);
    Task<ConcertDto> GetDetailsByApplicationIdAsync(int applicationId);
    Task<IEnumerable<ConcertSummaryDto>> GetUpcomingByVenueIdAsync(int id);
    Task<IEnumerable<ConcertSummaryDto>> GetUpcomingByArtistIdAsync(int id);
    Task<Result<ConcertEntity>> CreateDraftAsync(int applicationId);
    Task<ConcertUpdateResponse> UpdateAsync(int id, UpdateConcertRequest request);
    Task<ConcertPostResponse> PostAsync(int id, UpdateConcertRequest request);
    Task<IEnumerable<ConcertSummaryDto>> GetHistoryByArtistIdAsync(int id);
    Task<IEnumerable<ConcertSummaryDto>> GetHistoryByVenueIdAsync(int id);
    Task<IEnumerable<ConcertSummaryDto>> GetUnpostedByArtistIdAsync(int id);
    Task<IEnumerable<ConcertSummaryDto>> GetUnpostedByVenueIdAsync(int id);
}
