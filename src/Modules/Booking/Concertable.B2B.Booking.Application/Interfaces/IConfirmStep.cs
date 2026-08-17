using Concertable.B2B.Application.Contracts;
using Concertable.B2B.Booking.Application.DTOs;

namespace Concertable.B2B.Booking.Application.Interfaces;

internal interface IConfirmStep
{
    Task<BookingDto> ExecuteAsync(
        AcceptedApplication application,
        CancellationToken ct = default);
}

internal interface IStepResolver<TStep>
    where TStep : class
{
    TStep Resolve(DealType dealType);
}
