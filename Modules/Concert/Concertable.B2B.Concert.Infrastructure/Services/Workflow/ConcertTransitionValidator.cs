using Concertable.B2B.Concert.Application.Workflow;
using Concertable.B2B.Concert.Domain.Enums;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow;

internal sealed class ConcertTransitionValidator : IConcertTransitionValidator
{
    private readonly ConcertStage[] sequence;

    public ConcertTransitionValidator(ConcertStage[] sequence)
    {
        this.sequence = sequence;
    }

    public bool CanTransitionTo(ConcertStage from, ConcertStage to)
    {
        var fi = Array.IndexOf(sequence, from);
        var ti = Array.IndexOf(sequence, to);
        return fi >= 0 && ti == fi + 1;
    }
}
