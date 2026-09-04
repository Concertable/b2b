using Concertable.Contracts.Enums;

namespace Concertable.B2B.Concert.Domain.ValueObjects;

public sealed record ConcertDraft(
    string Name,
    string About,
    IReadOnlyCollection<Genre> Genres);
