using Concertable.B2B.Booking.Contracts.Events;
using Concertable.B2B.Concert.Contracts.Events;
using Concertable.B2B.IntegrationTests.Fixtures;
using Concertable.B2B.Opportunity.Domain.Entities;
using Concertable.B2B.Opportunity.Infrastructure.Data;
using Concertable.Messaging.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Concertable.B2B.Opportunity.IntegrationTests;

[Collection("Integration")]
public sealed class OpportunityCancellationIntegrationEventHandlerTests : IAsyncLifetime
{
    private readonly OpportunityApiFixture fixture;

    public OpportunityCancellationIntegrationEventHandlerTests(OpportunityApiFixture fixture, ITestOutputHelper output)
    {
        this.fixture = fixture;
        fixture.AttachOutput(output);
    }

    public Task InitializeAsync() => fixture.ResetAsync();
    public Task DisposeAsync() { fixture.DetachOutput(); return Task.CompletedTask; }

    [Fact]
    public async Task HandleAsync_BookingCancelledForFilledOpportunity_Reopens()
    {
        var opportunityId = await MarkFilledAsync();
        await using var scope = fixture.Services.CreateAsyncScope();
        var handler = scope.ServiceProvider
            .GetServices<IIntegrationEventHandler<BookingCancelledEvent>>()
            .Single(value => value.GetType().Name == "OpportunityCancellationIntegrationEventHandler");

        await handler.HandleAsync(
            new BookingCancelledEvent(1, 1, opportunityId),
            MessageEnvelope.Create<BookingCancelledEvent>(DateTimeOffset.UtcNow));

        var reopened = await ReadStateAsync(opportunityId);
        Assert.Equal(OpportunityState.Open, reopened);
    }

    [Fact]
    public async Task HandleAsync_ReplayedMessageId_IsNoOp()
    {
        var opportunityId = await MarkFilledAsync();
        await using var scope = fixture.Services.CreateAsyncScope();
        var handler = scope.ServiceProvider
            .GetServices<IIntegrationEventHandler<ConcertCancelledEvent>>()
            .Single(value => value.GetType().Name == "OpportunityCancellationIntegrationEventHandler");
        var envelope = MessageEnvelope.Create<ConcertCancelledEvent>(DateTimeOffset.UtcNow);
        var cancelled = new ConcertCancelledEvent(1, 1, opportunityId);
        await handler.HandleAsync(cancelled, envelope);
        await MarkFilledAsync(opportunityId);

        await handler.HandleAsync(cancelled, envelope);

        var stillFilled = await ReadStateAsync(opportunityId);
        Assert.Equal(OpportunityState.Filled, stillFilled);
    }

    private async Task<int> MarkFilledAsync()
    {
        var opportunityId = await fixture.Opportunities.Select(value => value.Id).FirstAsync();
        await MarkFilledAsync(opportunityId);
        return opportunityId;
    }

    private async Task MarkFilledAsync(int opportunityId)
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<OpportunityDbContext>();
        var opportunity = await context.Opportunities.SingleAsync(value => value.Id == opportunityId);
        opportunity.MarkFilled();
        await context.SaveChangesAsync();
    }

    private async Task<OpportunityState> ReadStateAsync(int opportunityId)
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<OpportunityDbContext>();
        return (await context.Opportunities.SingleAsync(value => value.Id == opportunityId)).State;
    }
}
