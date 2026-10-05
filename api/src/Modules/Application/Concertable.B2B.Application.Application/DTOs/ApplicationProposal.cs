using Concertable.B2B.Artist.Contracts;
using Concertable.B2B.Booking.Contracts;
using Concertable.B2B.Application.Domain.Lifecycle;

namespace Concertable.B2B.Application.Application.DTOs;

internal sealed record ApplicationProposal(
    int Id,
    Guid VenueTenantId,
    Guid ArtistTenantId,
    ArtistSummary Artist,
    OpportunityProposal Opportunity,
    ApplicationStatus Status,
    ApplicationState State)
{
    public BookingStatus? BookingStatus { get; init; }
    public ApplicationActionAvailability Actions { get; init; } = new(false, false, false, false, false, false);
}

internal sealed record ApplicationActionAvailability(
    bool Accept, bool Checkout, bool Decline, bool Cancel, bool Withdraw, bool Contract);
