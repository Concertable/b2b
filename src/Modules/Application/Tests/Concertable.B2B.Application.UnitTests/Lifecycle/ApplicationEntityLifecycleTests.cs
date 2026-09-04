using System.Net;
using Concertable.B2B.Application.Contracts;
using Concertable.B2B.Deal.Contracts;
using Concertable.B2B.Application.Domain.Entities;
using Concertable.B2B.Application.Domain.Lifecycle;
using Concertable.B2B.Application.Domain.ValueObjects;
using Concertable.B2B.Deal.Contracts.Enums;
using Concertable.Contracts.Enums;
using Concertable.Kernel;

namespace Concertable.B2B.Application.UnitTests;

public sealed class ApplicationEntityLifecycleTests
{
    [Fact]
    public void Cancel_WhenApplied_TransitionsToCancelled()
    {
        var application = ApplicationEntity.Create(
            1,
            2,
            DealType.FlatFee,
            Guid.NewGuid(),
            Guid.NewGuid());

        var result = application.Cancel();

        Assert.False(result.TryGetError(out _));
        Assert.Equal(ApplicationState.Cancelled, application.State);
    }

    [Fact]
    public void Cancel_WhenAlreadyCancelled_LeavesStateAndEventsUnchanged()
    {
        var application = ApplicationEntity.Create(
            1,
            2,
            DealType.FlatFee,
            Guid.NewGuid(),
            Guid.NewGuid());
        Assert.False(application.Cancel().TryGetError(out _));
        var events = application.DomainEvents.ToArray();

        var result = application.Cancel();

        Assert.True(result.TryGetError(out var error));
        Assert.Equal(new TransitionError<ApplicationState, ApplicationTrigger>(ApplicationState.Cancelled, ApplicationTrigger.Cancel), error);
        Assert.Equal(ApplicationState.Cancelled, application.State);
        Assert.Equal(events, application.DomainEvents);
    }

    [Fact]
    public void Accept_WhenAlreadyAccepted_LeavesStateAcceptanceAndEventsUnchanged()
    {
        var application = ApplicationEntity.Create(
            1,
            2,
            DealType.FlatFee,
            Guid.NewGuid(),
            Guid.NewGuid());
        var operationId = application.BeginAcceptance();
        var accepted = CreateAcceptedApplication(application, operationId);
        Assert.False(application.Accept(accepted).TryGetError(out _));
        var events = application.DomainEvents.ToArray();

        var result = application.Accept(accepted);

        Assert.True(result.TryGetError(out var error));
        Assert.Equal(new TransitionError<ApplicationState, ApplicationTrigger>(ApplicationState.Accepted, ApplicationTrigger.Accept), error);
        Assert.Equal(ApplicationState.Accepted, application.State);
        Assert.Equal(operationId, application.AcceptanceOperationId);
        Assert.Equal(events, application.DomainEvents);
    }

    private static AcceptedApplication CreateAcceptedApplication(
        ApplicationEntity application,
        Guid operationId)
    {
        var signature = new ContractSignature(
            Guid.NewGuid(),
            new DateTime(2030, 1, 1, 12, 0, 0, DateTimeKind.Utc),
            IPAddress.Loopback,
            "tests",
            "Signatory",
            null);

        return new AcceptedApplication(new ApplicationAcceptanceSnapshot(
            operationId,
            new ApplicationSnapshot(
                application.Id,
                new ArtistSnapshot(application.ArtistId, application.ArtistTenantId, "Artist"),
                new OpportunitySnapshot(
                    application.OpportunityId,
                    new VenueSnapshot(3, application.VenueTenantId, "Venue"),
                    new DateTime(2030, 1, 1, 19, 0, 0, DateTimeKind.Utc),
                    new DateTime(2030, 1, 1, 22, 0, 0, DateTimeKind.Utc),
                    [Genre.Rock])),
            new ContractSnapshot(
                PaymentMethod.Transfer,
                "Terms",
                "1",
                "2026-09",
                new PaymentCommitment("escrow-hold", $"app:{application.Id}"),
                signature,
                signature,
                new FlatFeeTerms(100m))));
    }
}
