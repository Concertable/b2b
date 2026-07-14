using Concertable.B2B.Concert.Application.Renderers;

namespace Concertable.B2B.Concert.UnitTests.Renderers;

public sealed class DealTermsRendererTests
{
    private readonly DealTermsRenderer renderer = new(
        new FlatFeeTermsRenderer(),
        new DoorSplitTermsRenderer(),
        new VersusTermsRenderer(),
        new VenueHireTermsRenderer());

    [Fact]
    public void Render_ShouldStateFlatFee_ForFlatFeeContract()
    {
        var contract = new FlatFeeDeal { PaymentMethod = PaymentMethod.Transfer, Fee = 500m };

        var text = renderer.Render(contract);

        Assert.Equal("The venue pays the artist a flat fee of £500.00.", text);
    }

    [Fact]
    public void Render_ShouldStateDoorPercent_ForDoorSplitContract()
    {
        var contract = new DoorSplitDeal { PaymentMethod = PaymentMethod.Cash, ArtistDoorPercent = 70m };

        var text = renderer.Render(contract);

        Assert.Equal("The artist receives 70% of door revenue.", text);
    }

    [Fact]
    public void Render_ShouldStateGuaranteePlusDoorPercent_ForVersusContract()
    {
        var contract = new VersusDeal { PaymentMethod = PaymentMethod.Cash, Guarantee = 200m, ArtistDoorPercent = 62.5m };

        var text = renderer.Render(contract);

        Assert.Equal("The artist receives a guarantee of £200.00 plus 62.5% of door revenue.", text);
    }

    [Fact]
    public void Render_ShouldStateHireFee_ForVenueHireContract()
    {
        var contract = new VenueHireDeal { PaymentMethod = PaymentMethod.Transfer, HireFee = 300m };

        var text = renderer.Render(contract);

        Assert.Equal("The artist pays the venue a hire fee of £300.00.", text);
    }
}
