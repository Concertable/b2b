using Concertable.B2B.Application.Contracts;
using Concertable.B2B.Booking.Application.DTOs;
using Concertable.B2B.Booking.Domain.Entities;

namespace Concertable.B2B.Booking.Application.Interfaces;

internal interface IConfirmStep
{
    Task<BookingDto> ExecuteAsync(
        AcceptedApplication application,
        CancellationToken ct = default);
}

internal interface ICancelStep
{
    Task ExecuteAsync(BookingEntity booking, CancellationToken ct = default);
}

internal interface IBookingConfirmationExecutor
{
    Task<BookingDto> ExecuteAsync(
        AcceptedApplication application,
        CancellationToken ct = default);
}

internal interface IBookingCancellationExecutor
{
    Task ExecuteAsync(BookingEntity booking, CancellationToken ct = default);
}

internal interface IBookingDealStrategyFactory<TStrategy>
    where TStrategy : class
{
    TStrategy Create(DealType dealType);
}
