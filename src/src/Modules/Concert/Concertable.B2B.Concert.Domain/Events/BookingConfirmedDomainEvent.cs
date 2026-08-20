using Concertable.Kernel;
using Concertable.Kernel.ValueObjects;

namespace Concertable.B2B.Concert.Domain.Events;

/// <summary>Raised when a booking is confirmed into a concert draft; the pre-commit handler stages the
/// booking-confirmation email so it commits atomically with the booking. Carries the counterparties'
/// display names and period so the handler renders without re-reading the booking graph mid-save.</summary>
public sealed record BookingConfirmedDomainEvent(
    Guid VenueTenantId,
    string VenueName,
    Guid ArtistTenantId,
    string ArtistName,
    DateRange Period) : IDomainEvent;
