namespace Concertable.B2B.Artist.Application.DTOs;

internal sealed record MonthlyRevenuePoint(DateOnly Month, long GrossCents, long NetCents, int Count);
