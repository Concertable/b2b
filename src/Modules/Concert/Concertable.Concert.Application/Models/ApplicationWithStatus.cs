namespace Concertable.Concert.Application.Models;

internal class ApplicationWithStatus
{
    public required ApplicationEntity Application { get; set; }
    public bool HasConcert { get; set; }
}
