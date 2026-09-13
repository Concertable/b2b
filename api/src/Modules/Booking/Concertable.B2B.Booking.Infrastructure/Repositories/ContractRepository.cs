using Concertable.B2B.Booking.Domain.Entities;
using Concertable.B2B.Booking.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Booking.Infrastructure.Repositories;

internal sealed class ContractRepository : VenueArtistTenantScopedRepository<ContractEntity>, IContractRepository
{
    private readonly BookingDbContext context;

    public ContractRepository(BookingDbContext context) : base(context) =>
        this.context = context;

    public Task<ContractEntity?> GetByApplicationIdAsync(
        int applicationId,
        CancellationToken ct = default) =>
        (from contract in context.Contracts
         join booking in context.Bookings on contract.BookingId equals booking.Id
         where booking.ApplicationId == applicationId
         select contract)
        .SingleOrDefaultAsync(ct);

    public Task<int?> GetIdByApplicationIdAsync(
        int applicationId,
        CancellationToken ct = default) =>
        (from contract in context.Contracts
         join booking in context.Bookings on contract.BookingId equals booking.Id
         where booking.ApplicationId == applicationId
         select (int?)contract.Id)
        .SingleOrDefaultAsync(ct);

    public Task<ContractEntity?> GetByBookingIdAsync(
        int bookingId,
        CancellationToken ct = default) =>
        context.Contracts.SingleOrDefaultAsync(contract => contract.BookingId == bookingId, ct);
}
