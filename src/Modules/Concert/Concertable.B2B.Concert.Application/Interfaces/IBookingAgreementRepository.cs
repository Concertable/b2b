using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.DataAccess.Application;

namespace Concertable.B2B.Concert.Application.Interfaces;

internal interface IBookingAgreementRepository : IVenueArtistTenantScopedRepository<BookingAgreementEntity>;
