namespace Concertable.B2B.Concert.Contracts;

public sealed record SettlementContext(
    int ConcertId,
    string ConcertName,
    Guid VenueTenantId,
    Guid ArtistTenantId,
    string VenueName,
    string ArtistName);
