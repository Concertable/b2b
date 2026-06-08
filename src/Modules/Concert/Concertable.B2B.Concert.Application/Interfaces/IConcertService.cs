using Concertable.B2B.Concert.Application.DTOs;
using Concertable.B2B.Concert.Application.Requests;
using Concertable.B2B.Concert.Application.Responses;
using Concertable.B2B.Concert.Domain.Entities;
using FluentResults;

namespace Concertable.B2B.Concert.Application.Interfaces;

internal interface IConcertService
{
    Task<ConcertDetails> GetDetailsByIdAsync(int id);
    Task<ConcertDetails> GetDetailsByApplicationIdAsync(int applicationId);
    Task<IEnumerable<ConcertSummary>> GetUpcomingByVenueIdAsync(int id);
    Task<IEnumerable<ConcertSummary>> GetUpcomingByArtistIdAsync(int id);
    Task<Result<ConcertEntity>> CreateDraftAsync(int applicationId);
    Task<ConcertUpdateResponse> UpdateAsync(int id, UpdateConcertRequest request);
    Task PostAsync(int id, UpdateConcertRequest request);
    Task<IEnumerable<ConcertSummary>> GetHistoryByArtistIdAsync(int id);
    Task<IEnumerable<ConcertSummary>> GetHistoryByVenueIdAsync(int id);
    Task<IEnumerable<ConcertSummary>> GetUnpostedByArtistIdAsync(int id);
    Task<IEnumerable<ConcertSummary>> GetUnpostedByVenueIdAsync(int id);
}
