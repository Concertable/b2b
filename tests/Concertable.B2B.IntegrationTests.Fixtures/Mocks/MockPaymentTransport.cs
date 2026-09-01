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
    private IServiceScopeFactory? serviceScopeFactory;

    public IReadOnlyCollection<object> Commands => commands.ToArray();

    /// <summary>
    /// Only the commands that move money. This transport carries every outbound command the service sends,
    /// emails included, and those arrive by outbox dispatch — so asserting that nothing was charged against
    /// <see cref="Commands"/> races an unrelated dispatch.
    /// </summary>
    public IReadOnlyCollection<object> FinancialCommands =>
        commands.Where(value => OperationId(value) is not null).ToArray();
    public bool HasPendingCommand => commands.Any(value =>
        OperationId(value) is { } operationId && !completed.ContainsKey(operationId));

    public Task PublishAsync<TEvent>(
        TEvent @event,
        MessageEnvelope envelope,
        CancellationToken ct = default)
        where TEvent : IIntegrationEvent => serviceScopeFactory is null
            ? Task.CompletedTask
            : DispatchAsync(@event, envelope, serviceScopeFactory, ct);

    public async Task SendAsync<TCommand>(
        TCommand command,
        MessageEnvelope envelope,
        CancellationToken ct = default)
        where TCommand : IIntegrationCommand
    {
        commands.Enqueue(command);
        if (serviceScopeFactory is null)
            return;

        await using var scope = serviceScopeFactory.CreateAsyncScope();
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
        this.serviceScopeFactory = serviceScopeFactory;
    }

    public Task CompleteLatestAsync(IServiceScopeFactory serviceScopeFactory) =>
        CompleteLatestAsync(serviceScopeFactory, _ => true);

    public Task CompleteLatestAsync<TCommand>(IServiceScopeFactory serviceScopeFactory)
        where TCommand : IIntegrationCommand =>
        CompleteLatestAsync(serviceScopeFactory, command => command is TCommand);

    public Task CompleteLatestAcceptanceAsync(IServiceScopeFactory serviceScopeFactory) =>
        CompleteLatestAsync(serviceScopeFactory, command => command is CaptureEscrowCommand or DepositEscrowCommand);

    public Task RejectLatestAcceptanceAsync(IServiceScopeFactory serviceScopeFactory) =>
        RejectLatestAsync(serviceScopeFactory, command => command is CaptureEscrowCommand or DepositEscrowCommand);

    public async Task RejectLatestAsync(IServiceScopeFactory serviceScopeFactory) =>
        await RejectLatestAsync(serviceScopeFactory, _ => true);

    /// <summary>
    /// Rejects the latest pending <typeparamref name="TCommand"/>. Name the command whenever more than one
    /// operation can be pending: commands arrive by outbox dispatch, so "the latest" reached synchronously
    /// can still be an earlier operation the flow never completed.
    /// </summary>
    public Task RejectLatestAsync<TCommand>(IServiceScopeFactory serviceScopeFactory)
        where TCommand : IIntegrationCommand =>
        RejectLatestAsync(serviceScopeFactory, command => command is TCommand);

    private async Task CompleteLatestAsync(
        IServiceScopeFactory serviceScopeFactory,
        Func<object, bool> predicate)
    {
        var command = await WaitForPendingAsync(predicate);
        switch (command)
        {
            case CaptureEscrowCommand capture:
                await DispatchAsync(
                    new CaptureEscrowSucceededEvent(capture.OperationId, capture.BookingId, "pi_test"),
                    serviceScopeFactory);
                completed.TryAdd(capture.OperationId, 0);
                break;
            case DepositEscrowCommand deposit:
                await DispatchAsync(
                    new DepositEscrowSucceededEvent(deposit.OperationId, deposit.BookingId, "pi_test"),
                    serviceScopeFactory);
                completed.TryAdd(deposit.OperationId, 0);
                break;
            case RefundEscrowCommand refund:
                await DispatchAsync(
                    new RefundEscrowSucceededEvent(refund.OperationId, refund.BookingId, "re_test"),
                    serviceScopeFactory);
                completed.TryAdd(refund.OperationId, 0);
                break;
            default:
                throw new InvalidOperationException($"Unsupported financial command {command.GetType().Name}.");
        }
    }

    private async Task RejectLatestAsync(
        IServiceScopeFactory serviceScopeFactory,
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
                    serviceScopeFactory);
                completed.TryAdd(capture.OperationId, 0);
                break;
            case DepositEscrowCommand deposit:
                await DispatchAsync(
                    new DepositEscrowRejectedEvent(
                        deposit.OperationId,
                        deposit.BookingId,
                        "card_declined",
                        "Card was declined"),
                    serviceScopeFactory);
                completed.TryAdd(deposit.OperationId, 0);
                break;
            case RefundEscrowCommand refund:
                await DispatchAsync(
                    new RefundEscrowRejectedEvent(
                        refund.OperationId,
                        refund.BookingId,
                        "refund_failed",
                        "Refund failed"),
                    serviceScopeFactory);
                completed.TryAdd(refund.OperationId, 0);
                break;
            default:
                throw new InvalidOperationException($"Unsupported financial command {command.GetType().Name}.");
        }
    }

    public TCommand SingleCommand<TCommand>() => commands.OfType<TCommand>().Single();

    /// <summary>
    /// The waiting counterpart to <see cref="SingleCommand{TCommand}"/>. A command reaches this transport
    /// through outbox dispatch, which completes after the request that staged it has returned, so reading
    /// synchronously races the dispatcher.
    /// </summary>
    public async Task<TCommand> SingleCommandAsync<TCommand>() =>
        (await WaitForCommandsAsync<TCommand>(1)).Single();

    /// <summary>
    /// Whether an acceptance command arrives at all. The branch that consumes one must not be chosen by a
    /// synchronous read: the command reaches this transport through outbox dispatch, which completes after
    /// the accept request has returned.
    /// </summary>
    public async Task<bool> WaitForAcceptanceCommandAsync()
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(5);
        while (DateTimeOffset.UtcNow <= deadline)
        {
            if (commands.Any(value => value is CaptureEscrowCommand or DepositEscrowCommand))
                return true;

            await Task.Delay(100);
        }

        return false;
    }

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

    private static async Task DispatchAsync<TEvent>(TEvent @event, IServiceScopeFactory serviceScopeFactory)
        where TEvent : IIntegrationEvent
    {
        var envelope = MessageEnvelope.Create<TEvent>(DateTimeOffset.UtcNow);
        await DispatchAsync(@event, envelope, serviceScopeFactory, CancellationToken.None);
    }

    private static async Task DispatchAsync<TEvent>(
        TEvent @event,
        MessageEnvelope envelope,
        IServiceScopeFactory serviceScopeFactory,
        CancellationToken ct)
        where TEvent : IIntegrationEvent
    {
        await using var scope = serviceScopeFactory.CreateAsyncScope();
        foreach (var handler in scope.ServiceProvider.GetServices<IIntegrationEventHandler<TEvent>>())
            await handler.HandleAsync(@event, envelope, ct);
    }
}
