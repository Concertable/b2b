using System.Runtime.CompilerServices;
using Concertable.B2B.Concert.Domain.Lifecycle;

namespace Concertable.B2B.Concert.UnitTests.Lifecycle;

public sealed class LifecycleOwnershipBaselineTests
{
    private static readonly string ConcertModule = Path.Combine(
        "Concertable.B2B",
        "src",
        "Modules",
        "Concert");

    public static TheoryData<string, string[]> OwnershipEvidence => new()
    {
        {
            Path.Combine("Concertable.B2B", "src", "Concertable.B2B.Workers", "Functions", "ConcertFinishedFunction.cs"),
            ["IConcertCompletionRunner", "runner.RunAsync()"]
        },
        {
            ConcertPath("Concertable.B2B.Concert.Api", "Controllers", "ApplicationController.cs"),
            ["Apply(", "CanApply(", "CanAccept(", "ApplyCheckout(", "AcceptCheckout(", "Accept(", "Withdraw(", "GetFinancialOperation(", "Reject(", "Cancel(", "GetContract("]
        },
        {
            ConcertPath("Concertable.B2B.Concert.Api", "Controllers", "ConcertController.cs"),
            ["GetInvoice(", "Update(", "Post(", "Cancel(", "DeclareDoorRevenue("]
        },
        {
            ConcertPath("Concertable.B2B.Concert.Api", "Mappers", "ApplicationResponseMapper.cs"),
            ["IConcertWorkflowCapabilityRegistry", "IAcceptsCheckout", "Withdraw:", "Reject:", "Cancel:", "Contract:"]
        },
        {
            ConcertPath("Concertable.B2B.Concert.Api", "Mappers", "OpportunityResponseMapper.cs"),
            ["IConcertWorkflowCapabilityRegistry", "IAppliesCheckout"]
        },
        {
            InfrastructurePath("Services", "Workflow", "Executors", "AcceptExecutor.cs"),
            ["unitOfWork.ExecuteAsync", "outbox.ExecuteAsync", "Trigger.Accept", "contractIssuer.IssueAsync", "bookingAdvancer.AdvanceIfReadyAsync"]
        },
        {
            InfrastructurePath("Services", "Workflow", "BookingAdvancer.cs"),
            ["PaymentVerification.Verified", "PaymentVerification.Failed", "LifecycleState.Accepted", "LifecycleState.PaymentFailed"]
        },
        {
            InfrastructurePath("Services", "Workflow", "Executors", "CancelApplicationExecutor.cs"),
            ["Trigger.Cancel", "IApplicationCancelStep", "LifecycleState.Accepted", "LifecycleState.PaymentFailed"]
        },
        {
            InfrastructurePath("Services", "Workflow", "Executors", "WithdrawExecutor.cs"),
            ["Trigger.Withdraw", "IApplicationCancelStep", "LifecycleState.Accepted", "LifecycleState.PaymentFailed"]
        },
        {
            InfrastructurePath("Services", "Workflow", "Executors", "CancelExecutor.cs"),
            ["GetByIdWithBookingAsync", "concert.Booking.ApplicationId", "Trigger.Cancel", "workflow.Cancel.ExecuteAsync"]
        },
        {
            InfrastructurePath("Services", "Workflow", "Executors", "FinishExecutor.cs"),
            ["GetByIdWithBookingAsync", "Trigger.Finish", "workflow.Finish.ExecuteAsync", "invoiceIssuer.IssueAsync", "DeferredPendingTaxCompliance", "DeferredPendingSelfBillingAgreement"]
        },
        {
            InfrastructurePath("Services", "Workflow", "Executors", "SettlementExecutor.cs"),
            ["GetApplicationIdByIdAsync", "Trigger.SettlementPaymentSucceeded", "Trigger.SettlementPaymentFailed"]
        },
        {
            InfrastructurePath("Services", "Payment", "SettlementPaymentProcessor.cs"),
            ["TransactionTypes.Settlement", "IsInboxMessageProcessedAsync", "AddInboxMessage", "settlementExecutor.SucceededAsync"]
        },
        {
            InfrastructurePath("Services", "Payment", "SettlementPaymentFailedProcessor.cs"),
            ["TransactionTypes.Settlement", "IsInboxMessageProcessedAsync", "AddInboxMessage", "settlementExecutor.FailedAsync"]
        },
        {
            InfrastructurePath("Services", "Workflow", "Executors", "EscrowExecutor.cs"),
            ["Trigger.EscrowPaymentSucceeded", "LifecycleState.CancellationPending", "cancelStep.ExecuteAsync", "workflow.Book.ExecuteAsync"]
        },
        {
            InfrastructurePath("Services", "Workflow", "Steps", "CreateConcertDraftStep.cs"),
            ["IBookStep", "concertDraftService.CreateAsync"]
        },
        {
            DomainPath("Entities", "ApplicationEntity.cs"),
            ["AcceptanceOperationId", "CancellationOperationId", "State == LifecycleState.CancellationFailed"]
        },
        {
            DomainPath("Entities", "ConcertEntity.cs"),
            ["CreateDraft(", "Booking = booking", "BookingId = booking.Id"]
        },
        {
            DomainPath("Entities", "InvoiceEntity.cs"),
            ["BookingId", "BookingEntity Booking", "Booking = concert.Booking", "DealType = concert.Booking.Application.DealType"]
        },
        {
            InfrastructurePath("Services", "Payment", "FinancialOperationOutcomeProcessor.cs"),
            ["AcceptanceOperationId == operationId", "CancellationOperationId == operationId", "Trigger.RefundSucceeded", "Trigger.RefundFailed", "IsInboxMessageProcessedAsync", "AddInboxMessage"]
        }
    };

    [Fact]
    public void LifecycleState_CurrentBaseline_MatchesExactValues()
    {
        string[] expected =
        [
            "Applied",
            "Rejected",
            "Withdrawn",
            "Accepted",
            "PaymentFailed",
            "Booked",
            "AwaitingSettlement",
            "SettlementFailed",
            "CancellationPending",
            "CancellationFailed",
            "Complete",
            "Cancelled"
        ];

        var actual = Enum.GetNames<LifecycleState>();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Trigger_CurrentBaseline_MatchesExactValues()
    {
        string[] expected =
        [
            "Accept",
            "Reject",
            "Withdraw",
            "VerifyPaymentSucceeded",
            "VerifyPaymentFailed",
            "EscrowPaymentSucceeded",
            "EscrowPaymentFailed",
            "SettlementPaymentSucceeded",
            "SettlementPaymentFailed",
            "RefundSucceeded",
            "RefundFailed",
            "Finish",
            "Cancel"
        ];

        var actual = Enum.GetNames<Trigger>();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void WorkflowExecutors_CurrentBaseline_MatchesExactFiles()
    {
        string[] expected =
        [
            "AcceptExecutor.cs",
            "ApplicationExecutor.cs",
            "ApplyExecutor.cs",
            "CancelApplicationExecutor.cs",
            "CancelExecutor.cs",
            "EscrowExecutor.cs",
            "FinishExecutor.cs",
            "RejectExecutor.cs",
            "SettlementExecutor.cs",
            "VerifyExecutor.cs",
            "WithdrawExecutor.cs"
        ];
        var directory = Path.Combine(
            FindApiRoot(),
            InfrastructurePath("Services", "Workflow", "Executors"));

        var actual = Directory.GetFiles(directory, "*.cs")
            .Select(Path.GetFileName)
            .Order()
            .ToArray();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void PaymentProcessors_CurrentBaseline_MatchesExactFiles()
    {
        string[] expected =
        [
            "EscrowPaymentFailedProcessor.cs",
            "EscrowPaymentProcessor.cs",
            "FinancialOperationOutcomeProcessor.cs",
            "PaymentVerificationRecorder.cs",
            "SettlementPaymentFailedProcessor.cs",
            "SettlementPaymentProcessor.cs",
            "TicketSaleProcessor.cs",
            "VerifyPaymentFailedProcessor.cs",
            "VerifyPaymentProcessor.cs"
        ];
        var directory = Path.Combine(
            FindApiRoot(),
            InfrastructurePath("Services", "Payment"));

        var actual = Directory.GetFiles(directory, "*.cs")
            .Select(Path.GetFileName)
            .Order()
            .ToArray();

        Assert.Equal(expected, actual);
    }

    [Theory]
    [MemberData(nameof(OwnershipEvidence))]
    public void OwnershipEvidence_CurrentBaseline_RemainsPinned(string relativePath, string[] expectedTokens)
    {
        var source = File.ReadAllText(Path.Combine(FindApiRoot(), relativePath));

        foreach (var expectedToken in expectedTokens)
            Assert.Contains(expectedToken, source, StringComparison.Ordinal);
    }

    private static string ConcertPath(params string[] parts) =>
        Path.Combine([ConcertModule, .. parts]);

    private static string InfrastructurePath(params string[] parts) =>
        ConcertPath(["Concertable.B2B.Concert.Infrastructure", .. parts]);

    private static string DomainPath(params string[] parts) =>
        ConcertPath(["Concertable.B2B.Concert.Domain", .. parts]);

    private static string FindApiRoot([CallerFilePath] string sourcePath = "")
    {
        var directory = new DirectoryInfo(Path.GetDirectoryName(sourcePath)!);
        while (directory is not null && directory.Name != "api")
            directory = directory.Parent;

        return directory?.FullName ?? throw new DirectoryNotFoundException("Could not locate the api source root.");
    }
}
