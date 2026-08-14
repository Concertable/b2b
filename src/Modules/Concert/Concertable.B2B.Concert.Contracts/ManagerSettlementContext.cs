namespace Concertable.B2B.Concert.Contracts;

public sealed record ManagerSettlementContext(
    int BookingId,
    int ConcertId,
    string ConcertName,
    Guid VenueTenantId,
    Guid ArtistTenantId,
    string VenueName,
    string ArtistName);
