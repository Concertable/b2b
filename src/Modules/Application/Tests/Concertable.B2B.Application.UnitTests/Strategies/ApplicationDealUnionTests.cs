using Concertable.B2B.Application.Application.Strategies;
using Concertable.B2B.Application.Infrastructure.Extensions;
using Concertable.B2B.Application.Infrastructure.Strategies;
using Concertable.B2B.Deal.Contracts.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.Application.UnitTests.Strategies;

public sealed class ApplicationDealUnionTests
{
    [Fact]
    public void AddApplicationDealUnions_RegistersApplyCasesForEveryDealType()
    {
        var services = new ServiceCollection();

        services.AddApplicationDealUnions();

        AssertRegistration<IApplyStandard, StandardApply>(services, DealType.FlatFee);
        AssertRegistration<IApplyStandard, StandardApply>(services, DealType.DoorSplit);
        AssertRegistration<IApplyStandard, StandardApply>(services, DealType.Versus);
        AssertRegistration<IApplyPrepaid, PrepaidApply>(services, DealType.VenueHire);
    }

    private static void AssertRegistration<TStrategy, TImplementation>(
        IServiceCollection services,
        DealType dealType)
    {
        var descriptor = Assert.Single(services.Where(candidate =>
            candidate.IsKeyedService
            && candidate.ServiceType == typeof(TStrategy)
            && Equals(candidate.ServiceKey, dealType)));
        Assert.Equal(typeof(TImplementation), descriptor.KeyedImplementationType);
    }
}
