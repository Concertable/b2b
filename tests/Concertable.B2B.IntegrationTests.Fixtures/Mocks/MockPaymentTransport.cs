using System.Collections.Concurrent;
using Concertable.Messaging.Contracts;
using Concertable.Payment.Contracts;
using Concertable.Testing.Integration;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.IntegrationTests.Fixtures.Mocks;

public sealed class MockPaymentTransport : IBusTransport, IResettable
{
    private readonly ConcurrentQueue<object> commands = new();
    private readonly ConcurrentDictionary<Guid, byte> completed = new();
    private IServiceScopeFactory? scopeFactory;

    public IReadOnlyCollection<object> Commands => commands.ToArray();
    public bool HasPendingCommand => commands.Any(value =>
        OperationId(value) is { } operationId && !completed.ContainsKey(operationId));

    public Task PublishAsync<TEvent>(
        TEvent @event,
        MessageEnvelope envelope,
        CancellationToken ct = default)
        where TEvent : IIntegrationEvent => scopeFactory is null
            ? Task.CompletedTask
            : DispatchAsync(@event, envelope, scopeFactory, ct);

    public async Task SendAsync<TCommand>(
        TCommand command,
        MessageEnvelope envelope,
        CancellationToken ct = default)
        where TCommand : IIntegrationCommand
    {
        commands.Enqueue(command);
        if (scopeFactory is null)
            return;

        await using var scope = scopeFactory.CreateAsyncScope();
        var handlers = scope.ServiceProvider.GetServices<IIntegrationCommandHandler<TCommand>>().ToArray();
        if (handlers.Length == 0)
            return;
        if (handlers.Length > 1)
            throw new InvalidOperationException(
                $"Multiple handlers registered for command {typeof(TCommand).FullName}.");

        await handlers[0].HandleAsync(command, envelope, ct);
    }

    public void Connect(IServiceScopeFactory serviceScopeFactory)
    {
        scopeFactory = serviceScopeFactory;
    }

    public Task CompleteLatestAsync(IServiceScopeFactory scopeFactory) =>
        CompleteLatestAsync(scopeFactory, _ => true);

    public Task CompleteLatestAsync<TCommand>(IServiceScopeFactory scopeFactory)
        where TCommand : IIntegrationCommand =>
        CompleteLatestAsync(scopeFactory, command => command is TCommand);

    public Task CompleteLatestAcceptanceAsync(IServiceScopeFactory scopeFactory) =>
        CompleteLatestAsync(scopeFactory, command => command is CaptureEscrowCommand or DepositEscrowCommand);

    public Task RejectLatestAcceptanceAsync(IServiceScopeFactory scopeFactory) =>
        RejectLatestAsync(scopeFactory, command => command is CaptureEscrowCommand or DepositEscrowCommand);

    public async Task RejectLatestAsync(IServiceScopeFactory scopeFactory) =>
        await RejectLatestAsync(scopeFactory, _ => true);

    private async Task CompleteLatestAsync(
        IServiceScopeFactory scopeFactory,
        Func<object, bool> predicate)
    {
        var command = await WaitForPendingAsync(predicate);
        switch (command)
        {
            case CaptureEscrowCommand capture:
                await DispatchAsync(
                    new CaptureEscrowSucceededEvent(capture.OperationId, capture.BookingId, "pi_test"),
                    scopeFactory);
                completed.TryAdd(capture.OperationId, 0);
                break;
            case DepositEscrowCommand deposit:
                await DispatchAsync(
                    new DepositEscrowSucceededEvent(deposit.OperationId, deposit.BookingId, "pi_test"),
                    scopeFactory);
                completed.TryAdd(deposit.OperationId, 0);
                break;
            case RefundEscrowCommand refund:
                await DispatchAsync(
                    new RefundEscrowSucceededEvent(refund.OperationId, refund.BookingId, "re_test"),
                    scopeFactory);
                completed.TryAdd(refund.OperationId, 0);
                break;
            default:
                throw new InvalidOperationException($"Unsupported financial command {command.GetType().Name}.");
        }
    }

    private async Task RejectLatestAsync(
        IServiceScopeFactory scopeFactory,
        Func<object, bool> predicate)
    {
        var command = await WaitForPendingAsync(predicate);
        switch (command)
        {
            case CaptureEscrowCommand capture:
                await DispatchAsync(
                    new CaptureEscrowRejectedEvent(
                        capture.OperationId,
                        capture.BookingId,
                        "card_declined",
                        "Card was declined"),
                    scopeFactory);
                completed.TryAdd(capture.OperationId, 0);
                break;
            case DepositEscrowCommand deposit:
                await DispatchAsync(
                    new DepositEscrowRejectedEvent(
                        deposit.OperationId,
                        deposit.BookingId,
                        "card_declined",
                        "Card was declined"),
                    scopeFactory);
                completed.TryAdd(deposit.OperationId, 0);
                break;
            case RefundEscrowCommand refund:
                await DispatchAsync(
                    new RefundEscrowRejectedEvent(
                        refund.OperationId,
                        refund.BookingId,
                        "refund_failed",
                        "Refund failed"),
                    scopeFactory);
                completed.TryAdd(refund.OperationId, 0);
                break;
            default:
                throw new InvalidOperationException($"Unsupported financial command {command.GetType().Name}.");
        }
    }

    public TCommand SingleCommand<TCommand>() => commands.OfType<TCommand>().Single();

    public async Task<IReadOnlyCollection<TCommand>> WaitForCommandsAsync<TCommand>(int count)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(5);
        while (DateTimeOffset.UtcNow <= deadline)
        {
            var matches = commands.OfType<TCommand>().ToArray();
            if (matches.Length >= count)
                return matches;

            await Task.Delay(100);
        }

        throw new InvalidOperationException(
            $"Expected {count} {typeof(TCommand).Name} commands within 5 seconds.");
    }

    public void Reset()
    {
        commands.Clear();
        completed.Clear();
    }

    private async Task<object> WaitForPendingAsync(Func<object, bool> predicate)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(5);
        while (DateTimeOffset.UtcNow <= deadline)
        {
            var command = commands.LastOrDefault(value => predicate(value) &&
                OperationId(value) is { } operationId && !completed.ContainsKey(operationId));
            if (command is not null)
                return command;

            await Task.Delay(100);
        }

        throw new InvalidOperationException("No pending financial command was dispatched within 5 seconds.");
    }

    private static Guid? OperationId(object command) => command switch
    {
        CaptureEscrowCommand capture => capture.OperationId,
        DepositEscrowCommand deposit => deposit.OperationId,
        RefundEscrowCommand refund => refund.OperationId,
        _ => null
    };

    private static async Task DispatchAsync<TEvent>(TEvent @event, IServiceScopeFactory scopeFactory)
        where TEvent : IIntegrationEvent
    {
        var envelope = MessageEnvelope.Create<TEvent>(DateTimeOffset.UtcNow);
        await DispatchAsync(@event, envelope, scopeFactory, CancellationToken.None);
    }

    private static async Task DispatchAsync<TEvent>(
        TEvent @event,
        MessageEnvelope envelope,
        IServiceScopeFactory scopeFactory,
        CancellationToken ct)
        where TEvent : IIntegrationEvent
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        foreach (var handler in scope.ServiceProvider.GetServices<IIntegrationEventHandler<TEvent>>())
            await handler.HandleAsync(@event, envelope, ct);
    }
}
