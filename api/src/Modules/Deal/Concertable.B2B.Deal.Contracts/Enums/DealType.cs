namespace Concertable.B2B.Deal.Contracts.Enums;

public enum DealType
{
    FlatFee,
    DoorSplit,
    Versus,
    VenueHire
}

public static class DealTypeNames
{
    public const string FlatFee = "flatFee";
    public const string DoorSplit = "doorSplit";
    public const string Versus = "versus";
    public const string VenueHire = "venueHire";
}
