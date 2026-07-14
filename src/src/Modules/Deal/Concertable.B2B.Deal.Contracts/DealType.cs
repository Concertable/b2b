using System.Text.Json.Serialization;

namespace Concertable.B2B.Deal.Contracts;

[JsonConverter(typeof(JsonStringEnumConverter<DealType>))]
public enum DealType
{
    [JsonStringEnumMemberName(DealTypeNames.FlatFee)]
    FlatFee,
    [JsonStringEnumMemberName(DealTypeNames.DoorSplit)]
    DoorSplit,
    [JsonStringEnumMemberName(DealTypeNames.Versus)]
    Versus,
    [JsonStringEnumMemberName(DealTypeNames.VenueHire)]
    VenueHire
}

public static class DealTypeNames
{
    public const string FlatFee = "flatFee";
    public const string DoorSplit = "doorSplit";
    public const string Versus = "versus";
    public const string VenueHire = "venueHire";
}
