using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Concertable.Concert.Infrastructure")]
[assembly: InternalsVisibleTo("Concertable.Concert.Api")]
[assembly: InternalsVisibleTo("Concertable.Concert.IntegrationTests")]
[assembly: InternalsVisibleTo("Concertable.Concert.UnitTests")]
[assembly: InternalsVisibleTo("Concertable.Workers.UnitTests")]
[assembly: InternalsVisibleTo("Concertable.E2ETests.Api")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
// TEMPORARY: legacy Concertable.Infrastructure still hosts Payment + Ticket services that inject Concert.Application
// internals (IConcertRepository, IOpportunityRepository, IContractLoader, ITicketPaymentStrategy). Retires when
// Payment Stage 1 extracts those services into Concertable.Payment.Infrastructure.
[assembly: InternalsVisibleTo("Concertable.Infrastructure")]
// TEMPORARY: Concertable.B2B.Workers (ConcertFinishedFunction) injects IConcertRepository + ICompletionDispatcher.
// Retires when the function moves into Concert.Api or its own Concert-owned worker.
[assembly: InternalsVisibleTo("Concertable.B2B.Workers")]
// TEMPORARY: Concertable.B2B.Web (E2EEndpointExtensions injects ICompletionDispatcher; ServiceCollectionExtensions
// keyed-registers ITicketPaymentStrategy impls). Retires when those move into Concert.Api / Payment.Infrastructure.
[assembly: InternalsVisibleTo("Concertable.B2B.Web")]
