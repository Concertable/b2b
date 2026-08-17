using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.ReadModels;
using Concertable.B2B.DataAccess.Application;

namespace Concertable.B2B.Concert.Application.Interfaces;

internal interface IBookingRepository : IVenueArtistTenantScopedRepository<BookingEntity>
{
    Task<BookingEntity?> GetByApplicationIdAsync(int applicationId, CancellationToken ct = default);
    Task<BookingDraftContext?> GetDraftContextByIdAsync(int bookingId, CancellationToken ct = default);
    Task<BookingEntity?> GetByConcertIdAsync(int concertId, CancellationToken ct = default);
    Task<BookingEntity?> GetForSettlementByConcertIdAsync(int concertId);
    Task<int?> GetIdByConcertIdAsync(int concertId);
    Task<int?> GetApplicationIdByIdAsync(int bookingId, CancellationToken ct = default);
    Task<int?> GetDealIdByIdAsync(int bookingId);
}

internal sealed record BookingDraftContext(
    BookingEntity Booking,
    ArtistReadModel Artist,
    OpportunityEntity Opportunity,
    VenueReadModel Venue);
