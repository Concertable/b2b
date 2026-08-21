using Concertable.B2B.Application.Application.Errors;
using Concertable.B2B.Application.Contracts;
using Concertable.B2B.Application.Domain.Entities;
using Concertable.Contracts.Enums;

namespace Concertable.B2B.Application.Application.Steps;

internal sealed record AcceptedApplicationFacts(
    Guid OperationId,
    int ApplicationId,
    int OpportunityId,
    int ArtistId,
    int VenueId,
    Guid VenueTenantId,
    Guid ArtistTenantId,
    PaymentMethod PaymentMethod,
    DateTime StartDate,
    DateTime EndDate,
    IReadOnlyList<Genre> Genres,
    string ArtistName,
    string VenueName,
    string TermsText,
    string PlatformTermsVersion,
    Signature ArtistSignature,
    Signature VenueSignature);

internal interface IAcceptStep
{
    Result<AcceptedApplication, AcceptApplicationError> Create(
        AcceptedApplicationFacts facts,
        ApplicationEntity application,
        DealDto deal,
        string? paymentMethodId);
}

internal sealed class FlatFeeAcceptStep : IAcceptStep
{
    public Result<AcceptedApplication, AcceptApplicationError> Create(
        AcceptedApplicationFacts facts,
        ApplicationEntity application,
        DealDto deal,
        string? paymentMethodId)
    {
        var terms = (FlatFeeDealDto)deal;
        return new FlatFeeAcceptedApplication(
            facts.OperationId, facts.ApplicationId, facts.OpportunityId, facts.ArtistId,
            facts.VenueId, facts.VenueTenantId, facts.ArtistTenantId, facts.PaymentMethod,
            facts.StartDate, facts.EndDate, facts.Genres, facts.ArtistName, facts.VenueName, facts.TermsText,
            facts.PlatformTermsVersion, facts.ArtistSignature, facts.VenueSignature, terms.Fee);
    }
}

internal sealed class DoorSplitAcceptStep : IAcceptStep
{
    public Result<AcceptedApplication, AcceptApplicationError> Create(
        AcceptedApplicationFacts facts,
        ApplicationEntity application,
        DealDto deal,
        string? paymentMethodId)
    {
        if (string.IsNullOrWhiteSpace(paymentMethodId))
            return new AcceptApplicationError.PaymentMethodRequired();
        var terms = (DoorSplitDealDto)deal;
        return new DoorSplitAcceptedApplication(
            facts.OperationId, facts.ApplicationId, facts.OpportunityId, facts.ArtistId,
            facts.VenueId, facts.VenueTenantId, facts.ArtistTenantId, facts.PaymentMethod,
            facts.StartDate, facts.EndDate, facts.Genres, facts.ArtistName, facts.VenueName, facts.TermsText,
            facts.PlatformTermsVersion, facts.ArtistSignature, facts.VenueSignature,
            terms.ArtistDoorPercent, paymentMethodId, application.Verification);
    }
}

internal sealed class VersusAcceptStep : IAcceptStep
{
    public Result<AcceptedApplication, AcceptApplicationError> Create(
        AcceptedApplicationFacts facts,
        ApplicationEntity application,
        DealDto deal,
        string? paymentMethodId)
    {
        if (string.IsNullOrWhiteSpace(paymentMethodId))
            return new AcceptApplicationError.PaymentMethodRequired();
        var terms = (VersusDealDto)deal;
        return new VersusAcceptedApplication(
            facts.OperationId, facts.ApplicationId, facts.OpportunityId, facts.ArtistId,
            facts.VenueId, facts.VenueTenantId, facts.ArtistTenantId, facts.PaymentMethod,
            facts.StartDate, facts.EndDate, facts.Genres, facts.ArtistName, facts.VenueName, facts.TermsText,
            facts.PlatformTermsVersion, facts.ArtistSignature, facts.VenueSignature,
            terms.Guarantee, terms.ArtistDoorPercent, paymentMethodId, application.Verification);
    }
}

internal sealed class VenueHireAcceptStep : IAcceptStep
{
    public Result<AcceptedApplication, AcceptApplicationError> Create(
        AcceptedApplicationFacts facts,
        ApplicationEntity application,
        DealDto deal,
        string? paymentMethodId)
    {
        if (application is not PrepaidApplication prepaid)
            return new AcceptApplicationError.PaymentMethodRequired();
        var terms = (VenueHireDealDto)deal;
        return new VenueHireAcceptedApplication(
            facts.OperationId, facts.ApplicationId, facts.OpportunityId, facts.ArtistId,
            facts.VenueId, facts.VenueTenantId, facts.ArtistTenantId, facts.PaymentMethod,
            facts.StartDate, facts.EndDate, facts.Genres, facts.ArtistName, facts.VenueName, facts.TermsText,
            facts.PlatformTermsVersion, facts.ArtistSignature, facts.VenueSignature,
            terms.HireFee, prepaid.PaymentMethodId);
    }
}
