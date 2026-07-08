using Concertable.B2B.Concert.Application.DTOs;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.Lifecycle;

namespace Concertable.B2B.Concert.Application.Mappers;

internal static class ApplicationMappers
{
    public static ApplicationStatus ToStatus(this LifecycleState state) => state switch
    {
        LifecycleState.Applied => ApplicationStatus.Pending,
        LifecycleState.Rejected => ApplicationStatus.Rejected,
        LifecycleState.Withdrawn => ApplicationStatus.Withdrawn,
        LifecycleState.Cancelled => ApplicationStatus.Cancelled,
        LifecycleState.Accepted
            or LifecycleState.PaymentFailed
            or LifecycleState.Booked
            or LifecycleState.AwaitingSettlement
            or LifecycleState.SettlementFailed
            or LifecycleState.Complete => ApplicationStatus.Accepted,
        _ => throw new ArgumentOutOfRangeException(nameof(state), state, null)
    };

    public static ArtistSummary ToArtistSummary(this ApplicationEntity application) => new()
    {
        Id = application.Artist.Id,
        Name = application.Artist.Name,
        Avatar = application.Artist.Avatar,
        Genres = application.Artist.Genres.Select(g => g.Genre)
    };
}
