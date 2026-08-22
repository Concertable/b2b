namespace Concertable.B2B.Venue.Application.DTOs;

internal sealed record MonthlyRevenuePoint(DateOnly Month, long GrossCents, long NetCents, int Count);

internal sealed record Settlement(
    int Id,
    int ConcertId,
    string ConcertName,
    DateTime At,
    long AmountCents,
    string CounterpartyName,
    SettlementDirection Direction);

internal enum SettlementDirection
{
    In,
    Out
}
