using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Concertable.B2B.Concert.Api")]
[assembly: InternalsVisibleTo("Concertable.B2B.Concert.IntegrationTests")]
[assembly: InternalsVisibleTo("Concertable.B2B.Concert.UnitTests")]
[assembly: InternalsVisibleTo("Concertable.B2B.Workers.UnitTests")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
// TEMPORARY: Concertable.B2B.Web injects internal WebhookService (Concert.Infrastructure.Services.Webhook).
// Retires when Webhook routing moves into Concert.Api or a Payment-owned host.
[assembly: InternalsVisibleTo("Concertable.B2B.Web")]
