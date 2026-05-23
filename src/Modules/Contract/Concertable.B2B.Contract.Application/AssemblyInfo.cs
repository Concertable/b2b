using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Concertable.B2B.Contract.Infrastructure")]
[assembly: InternalsVisibleTo("Concertable.B2B.Contract.Api")]
// Ride-along (Â§3.3): Concert.Infrastructure applies ContractEntityConfiguration on ConcertDbContext.
[assembly: InternalsVisibleTo("Concertable.B2B.Concert.Infrastructure")]
[assembly: InternalsVisibleTo("Concertable.Workers.UnitTests")]
[assembly: InternalsVisibleTo("Concertable.E2ETests.Api")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
