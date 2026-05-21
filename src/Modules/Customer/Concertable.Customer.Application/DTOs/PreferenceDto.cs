namespace Concertable.Customer.Application.DTOs;

internal record PreferenceDto
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public int RadiusKm { get; set; }
    public IReadOnlyList<Genre> Genres { get; set; } = [];
}
