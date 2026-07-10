using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Concert.Infrastructure.Repositories;

internal sealed class BookingAgreementRepository : VenueArtistTenantScopedRepository<BookingAgreementEntity>, IBookingAgreementRepository
{
    public BookingAgreementRepository(ConcertDbContext context) : base(context) { }

    public Task<BookingAgreementEntity?> GetByApplicationIdAsync(int applicationId, CancellationToken ct = default) =>
        context.BookingAgreements
            .FirstOrDefaultAsync(a => a.Booking.ApplicationId == applicationId, ct);

    public Task<BookingAgreementEntity?> GetByConcertIdAsync(int concertId, CancellationToken ct = default) =>
        context.BookingAgreements
            .FirstOrDefaultAsync(a => context.Concerts.Any(c => c.Id == concertId && c.BookingId == a.BookingId), ct);

    public Task<BookingAgreementEntity?> GetByBookingIdIgnoringTenantAsync(int bookingId, CancellationToken ct = default) =>
        context.BookingAgreements
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(a => a.BookingId == bookingId, ct);

    public Task<int?> GetIdByApplicationIdAsync(int applicationId, CancellationToken ct = default) =>
        context.BookingAgreements
            .Where(a => a.Booking.ApplicationId == applicationId)
            .Select(a => (int?)a.Id)
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyDictionary<int, int>> GetIdsByApplicationIdsAsync(
        IReadOnlyCollection<int> applicationIds, CancellationToken ct = default)
    {
        if (applicationIds.Count == 0)
            return new Dictionary<int, int>();

        var pairs = await context.BookingAgreements
            .Where(a => applicationIds.Contains(a.Booking.ApplicationId))
            .Select(a => new { ApplicationId = a.Booking.ApplicationId, AgreementId = a.Id })
            .ToListAsync(ct);

        return pairs.ToDictionary(p => p.ApplicationId, p => p.AgreementId);
    }
}
