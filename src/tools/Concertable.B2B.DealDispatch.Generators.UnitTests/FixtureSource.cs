namespace Concertable.B2B.DealDispatch.Generators.UnitTests;

internal static class FixtureSource
{
    public const string Infrastructure = """
        namespace Microsoft.Extensions.DependencyInjection
        {
            public interface IServiceCollection;

            public static class ServiceCollectionServiceExtensions
            {
                public static IServiceCollection AddScoped<TService>(
                    this IServiceCollection services)
                    where TService : class => services;

                public static IServiceCollection AddScoped<TService, TImplementation>(
                    this IServiceCollection services)
                    where TService : class
                    where TImplementation : class, TService => services;
            }
        }

        namespace Concertable.B2B.DealDispatch
        {
            [System.AttributeUsage(System.AttributeTargets.Interface)]
            internal sealed class DealStrategyFactoryContractAttribute : System.Attribute
            {
                public DealStrategyFactoryContractAttribute(System.Type markerType) { }
            }

            [System.AttributeUsage(System.AttributeTargets.Class)]
            internal sealed class GenerateDealStrategyFactoryAttribute : System.Attribute
            {
                public GenerateDealStrategyFactoryAttribute(
                    System.Type factoryType,
                    System.Type markerType) { }
            }

            [System.AttributeUsage(System.AttributeTargets.Class)]
            internal sealed class GenerateDealVariantFactoryAttribute : System.Attribute
            {
                public GenerateDealVariantFactoryAttribute(System.Type factoryType) { }
            }

            [System.AttributeUsage(System.AttributeTargets.Class)]
            internal sealed class DealVariantCasesAttribute : System.Attribute
            {
                public DealVariantCasesAttribute(params System.Type[] dealTypes) { }
            }
        }
        """;

    public const string Strategy = """
        namespace Fixture.Contracts
        {
            internal abstract record Deal;
            internal sealed record FlatFeeDeal : Deal;
            internal sealed record DoorSplitDeal : Deal;
        }

        namespace Fixture.Domain
        {
            internal abstract class DealEntity;
            internal sealed class FlatFeeDealEntity : DealEntity;
            internal sealed class DoorSplitDealEntity : DealEntity;
        }

        namespace Fixture.Application
        {
            using Concertable.B2B.DealDispatch;
            using Fixture.Contracts;
            using Fixture.Domain;

            internal interface IDealStrategy;

            [DealStrategyFactoryContract(typeof(IDealStrategy))]
            internal interface IDealStrategyFactory<TStrategy>
                where TStrategy : class, IDealStrategy
            {
                TStrategy Create(Deal deal);
                TStrategy Create(DealEntity entity);
            }

            internal interface IDealMapper : IDealStrategy;
            internal sealed class FlatFeeDealMapper : IDealMapper;
            internal sealed class DoorSplitDealMapper : IDealMapper;
        }

        namespace Fixture.Infrastructure
        {
            using Concertable.B2B.DealDispatch;
            using Fixture.Application;
            using Microsoft.Extensions.DependencyInjection;

            [GenerateDealStrategyFactory(
                typeof(IDealStrategyFactory<>),
                typeof(IDealStrategy))]
            internal static partial class DealStrategyRegistration;

            internal static class Startup
            {
                internal static IServiceCollection Add(IServiceCollection services) =>
                    services.AddDealStrategies();
            }
        }
        """;

    public const string Variant = """
        namespace Fixture.Contracts
        {
            internal abstract record Deal;
            internal sealed record FlatFeeDeal : Deal;
            internal sealed record DoorSplitDeal : Deal;
            internal sealed record VenueHireDeal : Deal;
        }

        namespace Fixture.Handlers
        {
            using Concertable.B2B.DealDispatch;
            using Fixture.Contracts;
            using Microsoft.Extensions.DependencyInjection;

            [DealVariantCases(typeof(FlatFeeDeal))]
            internal sealed class PaidAcceptHandler;

            [DealVariantCases(typeof(DoorSplitDeal), typeof(VenueHireDeal))]
            internal sealed class SimpleAcceptHandler;

            internal readonly struct AcceptHandler
            {
                internal AcceptHandler(PaidAcceptHandler handler) { }
                internal AcceptHandler(SimpleAcceptHandler handler) { }
            }

            internal interface IAcceptHandlerFactory
            {
                AcceptHandler Create(Deal deal);
            }

            [GenerateDealVariantFactory(typeof(IAcceptHandlerFactory))]
            internal static partial class AcceptHandlerRegistration;

            internal static class Startup
            {
                internal static IServiceCollection Add(IServiceCollection services) =>
                    services.AddAcceptHandlers();
            }
        }
        """;
}
