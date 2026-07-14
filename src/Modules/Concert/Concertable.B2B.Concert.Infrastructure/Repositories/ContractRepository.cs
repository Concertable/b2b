using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Concert.Infrastructure.Repositories;

internal sealed class ContractRepository : VenueArtistTenantScopedRepository<ContractEntity>, IContractRepository
{
    public ContractRepository(ConcertDbContext context) : base(context) { }

    public Task<ContractEntity?> GetByApplicationIdAsync(int applicationId, CancellationToken ct = default) =>
        context.Contracts
            .FirstOrDefaultAsync(a => a.Booking.ApplicationId == applicationId, ct);

    public Task<ContractEntity?> GetByConcertIdAsync(int concertId, CancellationToken ct = default) =>
        context.Contracts
            .FirstOrDefaultAsync(a => context.Concerts.Any(c => c.Id == concertId && c.BookingId == a.BookingId), ct);

    public Task<ContractEntity?> GetByBookingIdIgnoringTenantAsync(int bookingId, CancellationToken ct = default) =>
        context.Contracts
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(a => a.BookingId == bookingId, ct);

    public Task<int?> GetIdByApplicationIdAsync(int applicationId, CancellationToken ct = default) =>
        context.Contracts
            .Where(a => a.Booking.ApplicationId == applicationId)
            .Select(a => (int?)a.Id)
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyDictionary<int, int>> GetAgreementIdsByApplicationIdsAsync(
        IReadOnlyCollection<int> applicationIds, CancellationToken ct = default)
    {
        if (applicationIds.Count == 0)
            return new Dictionary<int, int>();

        var pairs = await context.Contracts
            .Where(a => applicationIds.Contains(a.Booking.ApplicationId))
            .Select(a => new { ApplicationId = a.Booking.ApplicationId, AgreementId = a.Id })
            .ToListAsync(ct);

        return pairs.ToDictionary(p => p.ApplicationId, p => p.AgreementId);
    }
}
