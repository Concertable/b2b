using System.Text.Json.Serialization;

namespace Concertable.B2B.Contract.Contracts;

[JsonDerivedType(typeof(FlatFeeContract), ContractTypeNames.FlatFee)]
[JsonDerivedType(typeof(DoorSplitContract), ContractTypeNames.DoorSplit)]
[JsonDerivedType(typeof(VersusContract), ContractTypeNames.Versus)]
[JsonDerivedType(typeof(VenueHireContract), ContractTypeNames.VenueHire)]
public interface IContract
{
    int Id { get; set; }
    PaymentMethod PaymentMethod { get; set; }
    ContractType ContractType { get; }
}
