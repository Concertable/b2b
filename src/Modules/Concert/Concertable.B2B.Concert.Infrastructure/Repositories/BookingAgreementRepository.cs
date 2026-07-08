using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Infrastructure.Data;

namespace Concertable.B2B.Concert.Infrastructure.Repositories;

internal sealed class BookingAgreementRepository : VenueArtistTenantScopedRepository<BookingAgreementEntity>, IBookingAgreementRepository
{
    public BookingAgreementRepository(ConcertDbContext context) : base(context) { }
}
