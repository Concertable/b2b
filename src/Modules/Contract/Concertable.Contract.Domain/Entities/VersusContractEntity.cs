namespace Concertable.Contract.Domain;

public class VersusContractEntity : ContractEntity
{
    private VersusContractEntity() { }

    public override ContractType ContractType => ContractType.Versus;
    public decimal Guarantee { get; private set; }
    public decimal ArtistDoorPercent { get; private set; }

    public static VersusContractEntity Create(decimal guarantee, decimal artistDoorPercent, PaymentMethod paymentMethod)
    {
        ValidateGuarantee(guarantee);
        ValidateArtistDoorPercent(artistDoorPercent);
        return new() { Guarantee = guarantee, ArtistDoorPercent = artistDoorPercent, PaymentMethod = paymentMethod };
    }

    public void Update(decimal guarantee, decimal artistDoorPercent, PaymentMethod paymentMethod)
    {
        ValidateGuarantee(guarantee);
        ValidateArtistDoorPercent(artistDoorPercent);
        Guarantee = guarantee;
        ArtistDoorPercent = artistDoorPercent;
        PaymentMethod = paymentMethod;
    }

    private static void ValidateGuarantee(decimal guarantee)
    {
        if (guarantee < 0)
            throw new DomainException("Guarantee must be zero or greater.");
    }

    private static void ValidateArtistDoorPercent(decimal artistDoorPercent)
    {
        if (artistDoorPercent < 0 || artistDoorPercent > 100)
            throw new DomainException("Artist door percent must be between 0 and 100.");
    }

    public decimal CalculateArtistShare(decimal totalRevenue)
        => Guarantee + (totalRevenue * (ArtistDoorPercent / 100));
}
