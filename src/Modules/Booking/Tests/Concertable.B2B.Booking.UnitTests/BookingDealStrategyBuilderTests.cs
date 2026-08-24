using Concertable.B2B.Booking.Application.Interfaces;
using Concertable.B2B.Booking.Infrastructure.Extensions;
using Concertable.B2B.Booking.Infrastructure.Services;
using Concertable.B2B.Deal.Contracts.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.Booking.UnitTests;

public sealed class BookingDealStrategyBuilderTests
{
    [Fact]
    public void AddScoped_DuplicateStrategyForDealType_ThrowsBeforeRegistration()
    {
        var services = new ServiceCollection();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddBookingDealStrategies(strategies =>
            {
                strategies.For(DealType.FlatFee)
                    .AddScoped<ITestStrategy, TestStrategy>()
                    .AddScoped<ITestStrategy, OtherTestStrategy>();
            }));

        Assert.Contains("ITestStrategy already has a registration for FlatFee", exception.Message);
        Assert.Empty(services);
    }

    [Fact]
    public void RequireAll_MissingDealTypes_ThrowsBeforeRegistration()
    {
        var services = new ServiceCollection();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddBookingDealStrategies(strategies =>
            {
                strategies.For(DealType.FlatFee)
                    .AddSingleton<ITestStrategy, TestStrategy>();
                strategies.RequireAll<ITestStrategy>();
            }));

        Assert.Contains("Missing: DoorSplit, Versus, VenueHire", exception.Message);
        Assert.Empty(services);
    }

    [Fact]
    public void AddBookingDealStrategies_DeclaresEveryConfirmAndCancelStrategy()
    {
        var expectedConfirm = new Dictionary<DealType, Type>
        {
            [DealType.FlatFee] = typeof(FlatFeeConfirmStep),
            [DealType.DoorSplit] = typeof(DoorSplitConfirmStep),
            [DealType.Versus] = typeof(VersusConfirmStep),
            [DealType.VenueHire] = typeof(VenueHireConfirmStep)
        };
        var expectedCancel = new Dictionary<DealType, Type>
        {
            [DealType.FlatFee] = typeof(EscrowCancelStep),
            [DealType.DoorSplit] = typeof(ImmediateCancelStep),
            [DealType.Versus] = typeof(ImmediateCancelStep),
            [DealType.VenueHire] = typeof(EscrowCancelStep)
        };
        var services = new ServiceCollection();
        services.AddBookingDealStrategies();

        var confirm = services
            .Where(service => service.ServiceType == typeof(IConfirmStep))
            .ToDictionary(
                service => Assert.IsType<DealType>(service.ServiceKey),
                service => service.KeyedImplementationType ??
                    throw new InvalidOperationException("Confirm strategy implementation type is missing."));
        var cancel = services
            .Where(service => service.ServiceType == typeof(ICancelStep))
            .ToDictionary(
                service => Assert.IsType<DealType>(service.ServiceKey),
                service => service.KeyedImplementationType ??
                    throw new InvalidOperationException("Cancel strategy implementation type is missing."));

        Assert.Equal(expectedConfirm, confirm);
        Assert.Equal(expectedCancel, cancel);
        Assert.DoesNotContain(
            services,
            service => service.ServiceType.IsGenericType &&
                service.ServiceType.GetGenericTypeDefinition().Name == "IStepResolver`1");
    }

    [Fact]
    public void Build_StrategyWithoutCoverageDeclaration_ThrowsBeforeRegistration()
    {
        var services = new ServiceCollection();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddBookingDealStrategies(strategies =>
                strategies.For(DealType.FlatFee)
                    .AddScoped<ITestStrategy, TestStrategy>()));

        Assert.Contains("Coverage has not been declared for: ITestStrategy", exception.Message);
        Assert.Empty(services);
    }

    [Fact]
    public void Build_ImplementationWithConflictingLifetimes_ThrowsBeforeRegistration()
    {
        var services = new ServiceCollection();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddBookingDealStrategies(strategies =>
            {
                strategies.For(DealType.FlatFee)
                    .AddSingleton<ITestStrategy, TestStrategy>();
                strategies.For(DealType.DoorSplit)
                    .AddScoped<ITestStrategy, TestStrategy>();
                strategies.RequireExactly<ITestStrategy>(DealType.FlatFee, DealType.DoorSplit);
            }));

        Assert.Contains("TestStrategy has conflicting strategy lifetimes: Singleton, Scoped", exception.Message);
        Assert.Empty(services);
    }

    private interface ITestStrategy;

    private sealed class TestStrategy : ITestStrategy;

    private sealed class OtherTestStrategy : ITestStrategy;
}
