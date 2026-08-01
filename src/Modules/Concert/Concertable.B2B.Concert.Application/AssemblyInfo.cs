using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Concertable.B2B.Concert.Infrastructure")]
[assembly: InternalsVisibleTo("Concertable.B2B.Concert.Api")]
[assembly: InternalsVisibleTo("Concertable.B2B.Concert.IntegrationTests")]
[assembly: InternalsVisibleTo("Concertable.B2B.Concert.UnitTests")]
[assembly: InternalsVisibleTo("Concertable.B2B.Workers.UnitTests")]
[assembly: InternalsVisibleTo("Concertable.E2ETests.Api")]
[assembly: InternalsVisibleTo("Concertable.B2B.E2ETests")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
// TEMPORARY: legacy Concertable.Infrastructure still hosts Payment + Ticket services that inject Concert.Application
// internals (IConcertRepository, IOpportunityRepository, ITicketPaymentStrategy). Retires when
// Payment Stage 1 extracts those services into Concertable.Payment.Infrastructure.
[assembly: InternalsVisibleTo("Concertable.Infrastructure")]
[assembly: InternalsVisibleTo("Concertable.B2B.Workers")]
[assembly: InternalsVisibleTo("Concertable.B2B.Web")]
