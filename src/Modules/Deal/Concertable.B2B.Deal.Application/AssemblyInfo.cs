using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Concertable.B2B.Deal.Infrastructure")]
[assembly: InternalsVisibleTo("Concertable.B2B.Deal.Api")]
// Ride-along (Â§3.3): Concert.Infrastructure applies DealEntityConfiguration on ConcertDbContext.
[assembly: InternalsVisibleTo("Concertable.B2B.Concert.Infrastructure")]
[assembly: InternalsVisibleTo("Concertable.B2B.Workers.UnitTests")]
[assembly: InternalsVisibleTo("Concertable.E2ETests.Api")]
[assembly: InternalsVisibleTo("Concertable.B2B.E2ETests")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
