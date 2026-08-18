using Concertable.B2B.Deal.Contracts.Enums;
using Concertable.Contracts.Enums;

namespace Concertable.B2B.Booking.Contracts;

public sealed record ConfirmedBooking(
    Guid OperationId,
    int BookingId,
    int ApplicationId,
    int OpportunityId,
    int ArtistId,
    int VenueId,
    Guid VenueTenantId,
    Guid ArtistTenantId,
    DealType DealType,
    bool RequiresDoorRevenue,
    DateTime StartDate,
    DateTime EndDate,
    IReadOnlyList<Genre> Genres,
    ConfirmedBookingTerms Terms);
