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
    public bool HasPendingAcceptance => commands.Any(value =>
        IsAcceptance(value) && OperationId(value) is { } operationId && !completed.ContainsKey(operationId));

    public bool HasSettledAcceptance => Settled(IsAcceptance) is not null;

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
        CompleteLatestAsync(serviceScopeFactory, IsAcceptance);

    /// <summary>
    /// Repeats the outcome a settled acceptance command already produced, under the id it carried, which is
    /// what the bus does and what the inbox recognises as a redelivery.
    /// </summary>
    public Task RedeliverLatestAcceptanceAsync(IServiceScopeFactory serviceScopeFactory) =>
        CompleteLatestAsync(serviceScopeFactory, IsAcceptance, redeliver: true);

    public Task RejectLatestAcceptanceAsync(IServiceScopeFactory serviceScopeFactory) =>
        RejectLatestAsync(serviceScopeFactory, IsAcceptance);

    public async Task DeferLatestAsync<TCommand>(IServiceScopeFactory serviceScopeFactory)
        where TCommand : IIntegrationCommand
    {
        var command = await WaitForPendingAsync(command => command is TCommand);
        if (command is not RefundEscrowCommand refund)
            throw new InvalidOperationException($"Only a refund can be deferred, not {command.GetType().Name}.");

        await DispatchAsync(
            new RefundEscrowDeferredEvent(refund.OperationId, refund.Reference),
            refund.OperationId,
            serviceScopeFactory);
    }

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
        Func<object, bool> predicate,
        bool redeliver = false)
    {
        var command = await WaitForPendingAsync(predicate, redeliver);
        switch (command)
        {
            case CaptureEscrowCommand capture:
                await DispatchAsync(
                    new CaptureEscrowSucceededEvent(capture.OperationId, capture.Reference),
                    capture.OperationId,
                    serviceScopeFactory);
                completed.TryAdd(capture.OperationId, 0);
                break;
            case DepositEscrowCommand deposit:
                await DispatchAsync(
                    new DepositEscrowSucceededEvent(deposit.OperationId, deposit.Reference),
                    deposit.OperationId,
                    serviceScopeFactory);
                completed.TryAdd(deposit.OperationId, 0);
                break;
            case RefundEscrowCommand refund:
                await DispatchAsync(
                    new RefundEscrowSucceededEvent(refund.OperationId, refund.Reference),
                    refund.OperationId,
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
                        capture.Reference,
                        "card_declined",
                        "Card was declined"),
                    capture.OperationId,
                    serviceScopeFactory);
                completed.TryAdd(capture.OperationId, 0);
                break;
            case DepositEscrowCommand deposit:
                await DispatchAsync(
                    new DepositEscrowRejectedEvent(
                        deposit.OperationId,
                        deposit.Reference,
                        "card_declined",
                        "Card was declined"),
                    deposit.OperationId,
                    serviceScopeFactory);
                completed.TryAdd(deposit.OperationId, 0);
                break;
            case RefundEscrowCommand refund:
                await DispatchAsync(
                    new RefundEscrowRejectedEvent(
                        refund.OperationId,
                        refund.Reference,
                        "refund_failed",
                        "Refund failed"),
                    refund.OperationId,
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
    /// <summary>
    /// Whether an acceptance command becomes pending within <paramref name="window"/>. A command reaches
    /// this transport by outbox dispatch, so a flow that has just staged one has not necessarily produced it
    /// yet and a synchronous read would miss it.
    /// </summary>
    public async Task<bool> WaitForPendingAcceptanceAsync(TimeSpan window)
    {
        var deadline = DateTimeOffset.UtcNow + window;
        while (DateTimeOffset.UtcNow <= deadline)
        {
            if (HasPendingAcceptance)
                return true;

            await Task.Delay(50);
        }

        return HasPendingAcceptance;
    }

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

    public async Task<IReadOnlyCollection<object>> SettledFinancialCommandsAsync(TimeSpan window)
    {
        var deadline = DateTimeOffset.UtcNow + window;
        while (DateTimeOffset.UtcNow <= deadline)
        {
            var financial = FinancialCommands;
            if (financial.Count > 0)
                return financial;

            await Task.Delay(100);
        }

        return FinancialCommands;
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

    private async Task<object> WaitForPendingAsync(Func<object, bool> predicate, bool redeliver = false)
    {
        if (redeliver && Settled(predicate) is { } alreadySettled)
            return alreadySettled;

        var deadline = DateTimeOffset.UtcNow.AddSeconds(5);
        while (DateTimeOffset.UtcNow <= deadline)
        {
            var command = commands.LastOrDefault(value => predicate(value) &&
                OperationId(value) is { } operationId && !completed.ContainsKey(operationId));
            if (command is not null)
                return command;

            await Task.Delay(100);
        }

        if (redeliver && Settled(predicate) is { } settled)
            return settled;

        throw new InvalidOperationException("No pending financial command was dispatched within 5 seconds.");
    }

    private object? Settled(Func<object, bool> predicate) =>
        commands.LastOrDefault(value => predicate(value)
            && OperationId(value) is { } operationId && completed.ContainsKey(operationId));

    private static bool IsAcceptance(object command) =>
        command is CaptureEscrowCommand or DepositEscrowCommand;

    private static Guid? OperationId(object command) => command switch
    {
        CaptureEscrowCommand capture => capture.OperationId,
        DepositEscrowCommand deposit => deposit.OperationId,
        RefundEscrowCommand refund => refund.OperationId,
        _ => null
    };

    private static async Task DispatchAsync<TEvent>(
        TEvent @event,
        Guid operationId,
        IServiceScopeFactory serviceScopeFactory)
        where TEvent : IIntegrationEvent
    {
        var envelope = new MessageEnvelope(
            PaymentOperationEnvelopes.StableId(operationId, typeof(TEvent)),
            MessageTypeAttribute.Resolve(typeof(TEvent)),
            DateTimeOffset.UtcNow);
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
