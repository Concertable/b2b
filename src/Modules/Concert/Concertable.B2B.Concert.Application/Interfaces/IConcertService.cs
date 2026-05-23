using Concertable.B2B.Concert.Application.DTOs;
using Concertable.B2B.Concert.Application.Requests;
using Concertable.B2B.Concert.Application.Responses;
using Concertable.B2B.Concert.Domain.Entities;
using FluentResults;

namespace Concertable.B2B.Concert.Application.Interfaces;

internal interface IConcertService
{
    Task<ConcertDto> GetDetailsByIdAsync(int id);
    Task<ConcertDto> GetDetailsByApplicationIdAsync(int applicationId);
    Task<IEnumerable<ConcertSummaryDto>> GetUpcomingByVenueIdAsync(int id);
    Task<IEnumerable<ConcertSummaryDto>> GetUpcomingByArtistIdAsync(int id);
    Task<Result<ConcertEntity>> CreateDraftAsync(int applicationId);
    Task<ConcertUpdateResponse> UpdateAsync(int id, UpdateConcertRequest request);
    Task PostAsync(int id, UpdateConcertRequest request);
    Task<IEnumerable<ConcertSummaryDto>> GetHistoryByArtistIdAsync(int id);
    Task<IEnumerable<ConcertSummaryDto>> GetHistoryByVenueIdAsync(int id);
    Task<IEnumerable<ConcertSummaryDto>> GetUnpostedByArtistIdAsync(int id);
    Task<IEnumerable<ConcertSummaryDto>> GetUnpostedByVenueIdAsync(int id);
}
