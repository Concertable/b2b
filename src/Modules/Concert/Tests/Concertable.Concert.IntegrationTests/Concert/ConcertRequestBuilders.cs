using Concertable.Concert.Application.Requests;

namespace Concertable.Concert.IntegrationTests.Concert;

internal static class ConcertRequestBuilders
{
    public static UpdateConcertRequest BuildPostRequest(
        string name = "Test Concert",
        string about = "Test Concert About",
        decimal price = 10.00m,
        int totalTickets = 100) => new()
    {
        Name = name,
        About = about,
        Price = price,
        TotalTickets = totalTickets
    };
}
