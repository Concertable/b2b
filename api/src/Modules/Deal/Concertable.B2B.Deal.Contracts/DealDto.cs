using System.Text.Json.Serialization;

namespace Concertable.B2B.Deal.Contracts;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(FlatFeeDealDto), DealTypeNames.FlatFee)]
[JsonDerivedType(typeof(DoorSplitDealDto), DealTypeNames.DoorSplit)]
[JsonDerivedType(typeof(VersusDealDto), DealTypeNames.Versus)]
[JsonDerivedType(typeof(VenueHireDealDto), DealTypeNames.VenueHire)]
public abstract record class DealDto
{
    public int Id { get; init; }
    public PaymentMethod PaymentMethod { get; init; }
    public abstract DealType DealType { get; }
    public abstract DealTerms Terms { get; }

    public abstract Result<DealEntity, ValidationErrors> ToEntity();
}

internal static class DealTypeNames
{
    public const string FlatFee = "flatFee";
    public const string DoorSplit = "doorSplit";
    public const string Versus = "versus";
    public const string VenueHire = "venueHire";
}
