using System.Text.Json.Serialization;

namespace Concertable.B2B.Concert.Application.DTOs;

internal sealed record ManagerConcertCard(
    int Id,
    string Name,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? BannerUrl,
    DateTime StartDate,
    DateTime EndDate,
    string CounterpartyName,
    int TicketsSold,
    int TicketsTotal,
    string Href);
