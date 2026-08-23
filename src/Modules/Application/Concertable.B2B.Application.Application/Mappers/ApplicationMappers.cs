using Concertable.B2B.Application.Application.DTOs;
using Concertable.B2B.Application.Domain.State;

namespace Concertable.B2B.Application.Application.Mappers;

internal static class ApplicationMappers
{
    extension(ApplicationState state)
    {
        public ApplicationStatus ToStatus() => state switch
        {
            ApplicationState.Applied => ApplicationStatus.Pending,
            ApplicationState.Rejected => ApplicationStatus.Rejected,
            ApplicationState.Withdrawn => ApplicationStatus.Withdrawn,
            ApplicationState.Accepted => ApplicationStatus.Accepted,
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, null)
        };
    }
}
