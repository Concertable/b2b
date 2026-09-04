using Concertable.B2B.Deal.Contracts;
using Concertable.B2B.Deal.Contracts.Enums;
using Concertable.Contracts.Enums;

namespace Concertable.B2B.Application.Contracts;

public sealed record ArtistSnapshot(
    int Id,
    Guid TenantId,
    string Name);

public sealed record VenueSnapshot(
    int Id,
    Guid TenantId,
    string Name);

public sealed record OpportunitySnapshot(
    int Id,
    VenueSnapshot Venue,
    DateTime StartDate,
    DateTime EndDate,
    IReadOnlyList<Genre> Genres);

public sealed record ApplicationSnapshot(
    int Id,
    ArtistSnapshot Artist,
    OpportunitySnapshot Opportunity);

public sealed record ContractSnapshot(
    PaymentMethod PaymentMethod,
    string TermsText,
    string PlatformTermsVersion,
    ContractSignature ArtistSignature,
    ContractSignature VenueSignature,
    DealTerms Terms);

public sealed record ApplicationAcceptanceSnapshot(
    Guid OperationId,
    ApplicationSnapshot Application,
    ContractSnapshot Contract);
