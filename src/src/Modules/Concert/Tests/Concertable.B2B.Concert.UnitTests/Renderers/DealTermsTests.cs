using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Infrastructure.Extensions;
using Concertable.Kernel.ValueObjects;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.Concert.UnitTests.Renderers;

public sealed class DealTermsTests : IDisposable
{
    private static readonly DateRange Period = new(
        new DateTime(2026, 9, 12, 19, 30, 0, DateTimeKind.Utc),
        new DateTime(2026, 9, 12, 22, 0, 0, DateTimeKind.Utc));

    private readonly ServiceProvider provider;
    private readonly IServiceScope scope;
    private readonly IDealTermsRenderer renderer;
    private readonly IDealTermsSerializer serializer;
    private readonly ITermsFingerprintCalculator fingerprint;

    public DealTermsTests()
    {
        var services = new ServiceCollection();
        services.AddConcertDealStrategies();

        this.provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });
        this.scope = this.provider.CreateScope();
        this.renderer = this.scope.ServiceProvider.GetRequiredService<IDealTermsRenderer>();
        this.serializer = this.scope.ServiceProvider.GetRequiredService<IDealTermsSerializer>();
        this.fingerprint = this.scope.ServiceProvider.GetRequiredService<ITermsFingerprintCalculator>();
    }

    public static TheoryData<IDeal, string, string, string> Characterizations =>
        new()
        {
        {
            new FlatFeeDeal { PaymentMethod = PaymentMethod.Transfer, Fee = 500m },
            "The venue pays the artist a flat fee of £500.00.",
            "Fee=500",
            "B360CD844AEAEC445D368309962F18A1EA0DACA28173D69D98F036798A92B91B"
        },
        {
            new DoorSplitDeal { PaymentMethod = PaymentMethod.Cash, ArtistDoorPercent = 70m },
            "The artist receives 70% of door revenue.",
            "ArtistDoorPercent=70",
            "6BE40145D99051414DC909ACA69EA7F1EE973415730E879D5789426FECF8257B"
        },
        {
            new VersusDeal { PaymentMethod = PaymentMethod.Cash, Guarantee = 200m, ArtistDoorPercent = 62.5m },
            "The artist receives a guarantee of £200.00 plus 62.5% of door revenue.",
            "Guarantee=200;ArtistDoorPercent=62.5",
            "7D2BBA1ABF4667137742D7AE6701F66F85DD08F3547BD4DE99AE078C3CA9971A"
        },
        {
            new VenueHireDeal { PaymentMethod = PaymentMethod.Transfer, HireFee = 300m },
            "The artist pays the venue a hire fee of £300.00.",
            "HireFee=300",
            "AE65DDB78BDBEB8C6E15800800771C61A3F38370BBBB435877140D8AF28B35D8"
        }
        };

    [Theory]
    [MemberData(nameof(Characterizations))]
    public void Render_DealType_ReturnsPinnedPresentation(
        IDeal deal,
        string expectedPresentation,
        string expectedSerialization,
        string expectedFingerprint)
    {
        var result = renderer.Render(deal);

        Assert.Equal(expectedPresentation, result);
    }

    [Theory]
    [MemberData(nameof(Characterizations))]
    public void Serialize_DealType_ReturnsPinnedCanonicalValue(
        IDeal deal,
        string expectedPresentation,
        string expectedSerialization,
        string expectedFingerprint)
    {
        var result = serializer.Serialize(deal);

        Assert.Equal(expectedSerialization, result);
    }

    [Theory]
    [MemberData(nameof(Characterizations))]
    public void Calculate_DealType_ReturnsPinnedFingerprint(
        IDeal deal,
        string expectedPresentation,
        string expectedSerialization,
        string expectedFingerprint)
    {
        var result = fingerprint.Calculate(deal, Period);

        Assert.Equal(expectedFingerprint, result);
    }

    public void Dispose()
    {
        this.scope.Dispose();
        this.provider.Dispose();
    }
}
