using System.Text.Json.Serialization;

namespace Concertable.B2B.Contract.Contracts;

[JsonConverter(typeof(JsonStringEnumConverter<ContractType>))]
public enum ContractType
{
    [JsonStringEnumMemberName(ContractTypeNames.FlatFee)]
    FlatFee,
    [JsonStringEnumMemberName(ContractTypeNames.DoorSplit)]
    DoorSplit,
    [JsonStringEnumMemberName(ContractTypeNames.Versus)]
    Versus,
    [JsonStringEnumMemberName(ContractTypeNames.VenueHire)]
    VenueHire
}

public static class ContractTypeNames
{
    public const string FlatFee = "flatFee";
    public const string DoorSplit = "doorSplit";
    public const string Versus = "versus";
    public const string VenueHire = "venueHire";
}
