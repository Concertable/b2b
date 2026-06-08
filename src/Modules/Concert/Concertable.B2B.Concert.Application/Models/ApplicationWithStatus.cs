using Concertable.B2B.Concert.Domain.Entities;

namespace Concertable.B2B.Concert.Application.Models;

internal sealed class ApplicationWithStatus
{
    public required ApplicationEntity Application { get; set; }
    public bool HasConcert { get; set; }
}
