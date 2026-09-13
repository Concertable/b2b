using Concertable.B2B.Application.Domain.Entities;

namespace Concertable.B2B.Application.Application.Models;

internal sealed class ApplicationWithStatus
{
    public required ApplicationEntity Application { get; set; }
    public bool HasConcert { get; set; }
}
