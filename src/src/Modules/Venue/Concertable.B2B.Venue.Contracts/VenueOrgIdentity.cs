namespace Concertable.B2B.Venue.Contracts;

/// <summary>A venue's recognisable public identity for a given tenant — the brand name plus its town/county —
/// used cross-module (Conversations) to attribute an inbound message to the counterparty org.</summary>
public sealed record VenueOrgIdentity(string Name, string? County, string? Town);
