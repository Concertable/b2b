using Concertable.B2B.Application.Application.DTOs;
using Concertable.B2B.Application.Domain.Lifecycle;

namespace Concertable.B2B.Application.Application.Mappers;

internal static class ApplicationMappers
{
    extension(State state)
    {
        public ApplicationStatus ToStatus() => state switch
        {
            State.Applied => ApplicationStatus.Pending,
            State.Rejected => ApplicationStatus.Rejected,
            State.Withdrawn => ApplicationStatus.Withdrawn,
            State.Accepted => ApplicationStatus.Accepted,
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, null)
        };
    }
}
