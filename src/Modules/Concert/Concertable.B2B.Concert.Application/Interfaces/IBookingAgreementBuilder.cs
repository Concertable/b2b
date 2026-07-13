using Concertable.B2B.Concert.Application.Requests;
using Concertable.B2B.Concert.Domain.Entities;

namespace Concertable.B2B.Concert.Application.Interfaces;

/// <summary>
/// Snapshots the agreed terms into a <see cref="BookingAgreementEntity"/> at Accept. Must run
/// inside the accept transition effect so the agreement commits atomically with the state change.
/// </summary>
internal interface IBookingAgreementBuilder
{
    Task BuildAsync(ApplicationEntity application, int bookingId, ESignatureRequest venueESignature);
}
