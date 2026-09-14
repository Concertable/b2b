using Concertable.B2B.Application.Contracts;
using Concertable.B2B.Booking.Domain.Entities;
using Concertable.B2B.Deal.Contracts;
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
        var snapshot = application.Snapshot;
        var booking = BookingEntity.Create(snapshot);
        booking.WithId(id);

        booking.MintContract(Mint(id, snapshot, createdAtUtc));
        var contract = booking.Contract
            .WithId(id)
            .With(nameof(ContractEntity.PdfBlobName), $"contracts/{id}-seed.pdf");

        if (confirmed)
        {
            booking.RecordFinancialConfirmation();
            booking.ClearDomainEvents();
        }

        // SQL Server allows IDENTITY_INSERT on one table at a time, so the seeder inserts bookings and
        // contracts in separate windows; leaving the navigation set would drag the contract into the
        // booking save and break the second window.
        booking.With(nameof(BookingEntity.Contract), null);
        return new BookingSeedAggregate(booking, contract);
    }

    private static ContractEntity Mint(
        int bookingId,
        ApplicationAcceptanceSnapshot snapshot,
        DateTime createdAtUtc) =>
        snapshot.Contract.Terms switch
        {
            FlatFeeTerms flatFee => FlatFeeContract.Create(bookingId, snapshot, flatFee, createdAtUtc),
            VenueHireTerms venueHire => VenueHireContract.Create(bookingId, snapshot, venueHire, createdAtUtc),
            DoorSplitTerms doorSplit => DoorSplitContract.Create(bookingId, snapshot, doorSplit, createdAtUtc),
            VersusTerms versus => VersusContract.Create(bookingId, snapshot, versus, createdAtUtc),
            var terms => throw new ArgumentOutOfRangeException(nameof(snapshot), terms, null)
        };
}

public sealed record BookingSeedAggregate(
    BookingEntity Booking,
    ContractEntity Contract);
