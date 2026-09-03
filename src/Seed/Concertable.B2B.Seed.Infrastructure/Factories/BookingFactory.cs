using Concertable.B2B.Application.Contracts;
using Concertable.B2B.Booking.Application.Mappers;
using Concertable.B2B.Booking.Domain.Entities;
using Concertable.B2B.Booking.Domain.ValueObjects;
using static Concertable.Seed.Identity.Extensions.EntityReflectionExtensions;

namespace Concertable.B2B.Seed.Infrastructure.Factories;

public static class BookingFactory
{
    public static BookingSeedAggregate Create(
        int id,
        AcceptedApplication application,
        DateTime createdAtUtc,
        bool confirmed)
    {
        var acceptance = application.ToBookingAcceptance();
        var booking = BookingEntity.Create(acceptance);
        booking.WithId(id);

        booking.MintContract(acceptance, createdAtUtc);
        var contract = booking.Contract
            .WithId(id)
            .With(nameof(ContractEntity.PdfBlobName), $"contracts/{id}-seed.pdf");

        if (confirmed)
        {
            booking.RecordFinancialConfirmation($"seed-financial-{id}");
            booking.ClearDomainEvents();
        }

        return new BookingSeedAggregate(booking, contract);
    }
}

public sealed record BookingSeedAggregate(
    BookingEntity Booking,
    ContractEntity Contract);
