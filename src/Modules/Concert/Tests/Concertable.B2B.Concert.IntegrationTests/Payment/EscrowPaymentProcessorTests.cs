using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.B2B.Concert.Infrastructure.Services.Payment;
using Concertable.B2B.IntegrationTests.Fixtures;
using Concertable.Kernel.DependencyInjection;
using Concertable.Messaging.Contracts;
using Concertable.Messaging.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Concertable.B2B.Concert.IntegrationTests.Payment;

[Collection("Integration")]
public sealed class EscrowPaymentProcessorTests : IAsyncLifetime
{
    private readonly ConcertApiFixture fixture;
    private readonly IScoped<ConcertTenantDbContext> scoped;

    public EscrowPaymentProcessorTests(ConcertApiFixture fixture, ITestOutputHelper output)
    {
        this.fixture = fixture;
        this.scoped = fixture.Services.GetRequiredService<IScoped<ConcertTenantDbContext>>();
        fixture.AttachOutput(output);
    }

    public Task InitializeAsync() => fixture.ResetAsync();
    public Task DisposeAsync() { fixture.DetachOutput(); return Task.CompletedTask; }

    [Fact]
    public async Task UnknownBookingEvents_AreAcknowledgedWithoutThrowing()
    {
        const int bookingId = int.MaxValue;
        var succeededEnvelope = Envelope<PaymentSucceededEvent>();
        var failedEnvelope = Envelope<PaymentFailedEvent>();
        var metadata = new Dictionary<string, string>
        {
            [PaymentMetadataKeys.Type] = TransactionTypes.Escrow,
            [PaymentMetadataKeys.BookingId] = bookingId.ToString()
        };

        await this.DispatchAsync(new PaymentSucceededEvent("pi_missing", metadata), succeededEnvelope);
        await this.DispatchAsync(new PaymentFailedEvent("pi_missing", "missing", "Missing booking", metadata), failedEnvelope);

        var processed = await this.scoped.RunAsync(context => context.Set<InboxMessageEntity>()
            .Where(message => message.MessageId == succeededEnvelope.MessageId || message.MessageId == failedEnvelope.MessageId)
            .Select(message => message.ConsumerName)
            .ToListAsync());

        Assert.Contains(nameof(EscrowPaymentProcessor), processed);
        Assert.Contains(nameof(EscrowPaymentFailedProcessor), processed);
    }

    private static MessageEnvelope Envelope<T>() =>
        new(Guid.NewGuid(), MessageTypeAttribute.Resolve(typeof(T)), DateTimeOffset.UtcNow);

    private Task DispatchAsync<TEvent>(TEvent @event, MessageEnvelope envelope)
        where TEvent : IIntegrationEvent =>
        this.fixture.Services.GetRequiredService<IScoped<IEnumerable<IIntegrationEventHandler<TEvent>>>>()
            .RunAsync(async handlers =>
            {
                foreach (var handler in handlers)
                    await handler.HandleAsync(@event, envelope);
            });
}
