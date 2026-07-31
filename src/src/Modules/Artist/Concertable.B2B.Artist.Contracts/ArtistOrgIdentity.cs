namespace Concertable.B2B.Artist.Contracts;

/// <summary>An artist's recognisable public identity for a given tenant — the brand name plus its town/county —
/// used cross-module (Conversations) to attribute an inbound message to the counterparty org.</summary>
public sealed record ArtistOrgIdentity(string Name, string County, string Town);
