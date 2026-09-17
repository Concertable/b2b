using Concertable.B2B.Booking.Application.Interfaces;
using Concertable.B2B.Booking.Contracts;
using Concertable.B2B.Booking.Infrastructure.Data;
using Concertable.B2B.Booking.Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Booking.Infrastructure.Services;

internal sealed class SubjectContractReader : ISubjectContractReader
{
    private readonly IBookingReadDbContext context;

    public SubjectContractReader(IBookingReadDbContext context)
    {
        this.context = context;
    }

    public async Task<IReadOnlyList<SubjectContractDto>> GetSubjectContractsAsync(IReadOnlySet<Guid> tenantIds, CancellationToken ct = default)
    {
        if (tenantIds.Count == 0)
            return [];

        var contracts = await context.Contracts
            .Where(c => tenantIds.Contains(c.VenueTenantId) || tenantIds.Contains(c.ArtistTenantId))
            .ToListAsync(ct);

        return contracts.Select(c => c.ToSubjectContractDto()).ToList();
    }
}
