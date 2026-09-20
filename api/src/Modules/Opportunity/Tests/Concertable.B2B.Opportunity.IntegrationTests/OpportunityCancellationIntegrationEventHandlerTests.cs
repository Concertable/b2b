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
        const int applicationId = 1;
        var opportunityId = await MarkFilledAsync(applicationId);
        await using var scope = fixture.Services.CreateAsyncScope();
        var handler = scope.ServiceProvider
            .GetServices<IIntegrationEventHandler<BookingCancelledEvent>>()
            .Single(value => value.GetType().Name == "OpportunityCancellationIntegrationEventHandler");

        await handler.HandleAsync(
            new BookingCancelledEvent(1, applicationId, opportunityId),
            MessageEnvelope.Create<BookingCancelledEvent>(DateTimeOffset.UtcNow));

        var reopened = await ReadAsync(opportunityId);
        Assert.Equal(OpportunityState.Open, reopened.State);
        Assert.Null(reopened.FilledByApplicationId);
    }

    [Fact]
    public async Task HandleAsync_ReplayedMessageId_IsNoOp()
    {
        const int applicationId = 1;
        var opportunityId = await MarkFilledAsync(applicationId);
        await using var scope = fixture.Services.CreateAsyncScope();
        var handler = scope.ServiceProvider
            .GetServices<IIntegrationEventHandler<ConcertCancelledEvent>>()
            .Single(value => value.GetType().Name == "OpportunityCancellationIntegrationEventHandler");
        var envelope = MessageEnvelope.Create<ConcertCancelledEvent>(DateTimeOffset.UtcNow);
        var cancelled = new ConcertCancelledEvent(1, applicationId, opportunityId);
        await handler.HandleAsync(cancelled, envelope);
        await MarkFilledAsync(opportunityId, 2);

        await handler.HandleAsync(cancelled, envelope);

        var stillFilled = await ReadAsync(opportunityId);
        Assert.Equal(OpportunityState.Filled, stillFilled.State);
        Assert.Equal(2, stillFilled.FilledByApplicationId);
    }

    [Fact]
    public async Task HandleAsync_DelayedCancellationForPreviousFill_DoesNotReopenCurrentFill()
    {
        const int previousApplicationId = 1;
        const int currentApplicationId = 2;
        var opportunityId = await MarkFilledAsync(previousApplicationId);
        await using var scope = fixture.Services.CreateAsyncScope();
        var handler = scope.ServiceProvider
            .GetServices<IIntegrationEventHandler<BookingCancelledEvent>>()
            .Single(value => value.GetType().Name == "OpportunityCancellationIntegrationEventHandler");

        await handler.HandleAsync(
            new BookingCancelledEvent(1, previousApplicationId, opportunityId),
            MessageEnvelope.Create<BookingCancelledEvent>(DateTimeOffset.UtcNow));
        await MarkFilledAsync(opportunityId, currentApplicationId);
        await handler.HandleAsync(
            new BookingCancelledEvent(1, previousApplicationId, opportunityId),
            MessageEnvelope.Create<BookingCancelledEvent>(DateTimeOffset.UtcNow));

        var stillFilled = await ReadAsync(opportunityId);
        Assert.Equal(OpportunityState.Filled, stillFilled.State);
        Assert.Equal(currentApplicationId, stillFilled.FilledByApplicationId);
    }

    private async Task<int> MarkFilledAsync(int applicationId)
    {
        var opportunityId = await fixture.Opportunities.Select(value => value.Id).FirstAsync();
        await MarkFilledAsync(opportunityId, applicationId);
        return opportunityId;
    }

    private async Task MarkFilledAsync(int opportunityId, int applicationId)
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<OpportunityPrivilegedDbContext>();
        var opportunity = await context.Opportunities.SingleAsync(value => value.Id == opportunityId);
        opportunity.MarkFilled(applicationId);
        await context.SaveChangesAsync();
    }

    private async Task<OpportunityEntity> ReadAsync(int opportunityId)
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<OpportunityPrivilegedDbContext>();
        return await context.Opportunities.AsNoTracking().SingleAsync(value => value.Id == opportunityId);
    }
}
