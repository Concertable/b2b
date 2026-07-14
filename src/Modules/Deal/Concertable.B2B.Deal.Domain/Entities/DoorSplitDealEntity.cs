namespace Concertable.B2B.Deal.Domain.Entities;

public sealed class DoorSplitDealEntity : DealEntity
{
    private DoorSplitDealEntity() { }

    public override DealType DealType => DealType.DoorSplit;
    public decimal ArtistDoorPercent { get; private set; }

    public static DoorSplitDealEntity Create(decimal artistDoorPercent, PaymentMethod paymentMethod)
    {
        ValidateArtistDoorPercent(artistDoorPercent);
        return new() { ArtistDoorPercent = artistDoorPercent, PaymentMethod = paymentMethod };
    }

    public void Update(decimal artistDoorPercent, PaymentMethod paymentMethod)
    {
        ValidateArtistDoorPercent(artistDoorPercent);
        ArtistDoorPercent = artistDoorPercent;
        PaymentMethod = paymentMethod;
    }

    private static void ValidateArtistDoorPercent(decimal artistDoorPercent)
    {
        if (artistDoorPercent < 0 || artistDoorPercent > 100)
            throw new DomainException("Artist door percent must be between 0 and 100.");
    }

    public decimal CalculateArtistShare(decimal totalRevenue)
        => totalRevenue * (ArtistDoorPercent / 100);
}
