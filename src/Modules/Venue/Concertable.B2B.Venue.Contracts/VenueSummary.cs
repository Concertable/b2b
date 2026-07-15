using System.ComponentModel;

namespace Concertable.B2B.Venue.Contracts;

[DisplayName(DisplayNames.Venue)]
public sealed record VenueSummary(int Id, string Name, string? Avatar, double Rating);
