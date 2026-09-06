using Xunit;
using DomainConcertState = Concertable.B2B.Concert.Domain.Lifecycle.ConcertState;
using DtoApplicationStatus = Concertable.B2B.Application.Application.DTOs.ApplicationStatus;
using InfrastructureReferences = Concertable.B2B.Infrastructure.Payments.PaymentOperationReferences;
using TestKitApplicationStatus = Concertable.B2B.TestKit.ApplicationStatus;
using TestKitConcertState = Concertable.B2B.TestKit.ConcertState;
using TestKitReferences = Concertable.B2B.TestKit.PaymentOperationReferences;

namespace Concertable.B2B.ArchitectureTests;

public sealed class TestKitMirrorTests
{
    [Fact]
    public void ConcertState_MirrorsTheDomainEnum() =>
        Assert.Equal(Members<DomainConcertState>(), Members<TestKitConcertState>());

    [Fact]
    public void ApplicationStatus_MirrorsTheDtoEnum() =>
        Assert.Equal(Members<DtoApplicationStatus>(), Members<TestKitApplicationStatus>());

    [Fact]
    public void PaymentOperationReferences_MirrorTheReferencesB2BMints()
    {
        Assert.Equal(InfrastructureReferences.EscrowType, TestKitReferences.EscrowType);
        Assert.Equal(InfrastructureReferences.SettlementType, TestKitReferences.SettlementType);
        Assert.Equal(
            InfrastructureReferences.Escrow(42).ClientReference,
            TestKitReferences.EscrowClientReference(42));
        Assert.Equal(
            InfrastructureReferences.Settlement(42).ClientReference,
            TestKitReferences.SettlementClientReference(42));
    }

    private static IEnumerable<(string Name, int Value)> Members<TEnum>()
        where TEnum : struct, Enum =>
        Enum.GetValues<TEnum>().Select(member => (member.ToString(), Convert.ToInt32(member)));
}
