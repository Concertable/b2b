using System.Text.Json.Serialization;

namespace Concertable.B2B.Deal.Contracts;

[JsonDerivedType(typeof(FlatFeeDeal), DealTypeNames.FlatFee)]
[JsonDerivedType(typeof(DoorSplitDeal), DealTypeNames.DoorSplit)]
[JsonDerivedType(typeof(VersusDeal), DealTypeNames.Versus)]
[JsonDerivedType(typeof(VenueHireDeal), DealTypeNames.VenueHire)]
public interface IDeal
{
    int Id { get; set; }
    PaymentMethod PaymentMethod { get; set; }
    DealType ContractType { get; }
}
