using Concertable.B2B.Concert.Application.Errors;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.ValueObjects;
using Concertable.B2B.Concert.Infrastructure.Services;
using Concertable.Contracts.Enums;
using Concertable.Kernel.ValueObjects;

namespace Concertable.B2B.Concert.UnitTests.Services;

public sealed class ConcertServiceSummaryShareTests
{
    private static readonly Guid UserId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid RecipientTenantId = Guid.Parse("55555555-5555-5555-5555-555555555555");

    [Fact]
    public void RecoverSummaryShareDuplicate_MatchingReceipt_ReplaysGrant()
    {
        var recovery = CreateRecovery();

        var result = ConcertService.RecoverSummaryShareDuplicate(
            recovery.Concert,
            recovery.Receipt,
            recovery.Fingerprint);

        Assert.True(result.TryGetValue(out var share));
        Assert.Equal(recovery.Grant.Id, share.GrantId);
    }

    [Fact]
    public void RecoverSummaryShareDuplicate_MismatchedReceipt_ReturnsRequestConflict()
    {
        var recovery = CreateRecovery();

        var result = ConcertService.RecoverSummaryShareDuplicate(
            recovery.Concert,
            recovery.Receipt,
            Sha256Digest.Of("different"));

        Assert.True(result.TryGetError(out var error));
        Assert.IsType<ShareConcertSummaryError.RequestConflict>(error);
    }

    [Fact]
    public void RecoverSummaryShareDuplicate_MatchingReceiptForARemovedShare_IsSuperseded()
    {
        var recovery = CreateRecovery();
        var concert = ConcertEntity.CreateDraft(
            ConfirmedBookings.FlatFee(),
            new ConcertDraft("Concert", "About", [Genre.Rock]),
            DateTime.UnixEpoch);

        var result = ConcertService.RecoverSummaryShareDuplicate(
            concert,
            recovery.Receipt,
            recovery.Fingerprint);

        Assert.True(result.TryGetError(out var error));
        Assert.IsType<ShareConcertSummaryError.Superseded>(error);
    }

    [Fact]
    public void RecoverSummaryShareDuplicate_MatchingReceiptWithMalformedOutcome_Throws()
    {
        var recovery = CreateRecovery();
        var receipt = ConcertCommandReceipt.Record(
            ConfirmedBookings.VenueTenantId,
            ConcertCommandReceipt.ShareSummaryOperation,
            Guid.NewGuid(),
            recovery.Fingerprint,
            "not-a-grant-id",
            DateTime.UnixEpoch);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            ConcertService.RecoverSummaryShareDuplicate(
                recovery.Concert,
                receipt,
                recovery.Fingerprint));

        Assert.Contains(receipt.Id.ToString(), exception.Message);
        Assert.Contains("invalid grant outcome", exception.Message);
    }

    [Fact]
    public void RecoverSummaryShareDuplicate_MissingReceipt_ReturnsAlreadyShared()
    {
        var recovery = CreateRecovery();

        var result = ConcertService.RecoverSummaryShareDuplicate(
            recovery.Concert,
            null,
            recovery.Fingerprint);

        Assert.True(result.TryGetError(out var error));
        Assert.IsType<ShareConcertSummaryError.AlreadyShared>(error);
    }

    private static Recovery CreateRecovery()
    {
        var concert = ConcertEntity.CreateDraft(
            ConfirmedBookings.FlatFee(),
            new ConcertDraft("Concert", "About", [Genre.Rock]),
            DateTime.UnixEpoch);
        var grantResult = concert.ShareSummary(
            ConfirmedBookings.VenueTenantId,
            UserId,
            RecipientTenantId,
            null,
            DateTime.UnixEpoch,
            null);
        Assert.True(grantResult.TryGetValue(out var grant));
        var fingerprint = Sha256Digest.Of(
            concert.Id,
            RecipientTenantId,
            null,
            null);
        var receipt = ConcertCommandReceipt.Record(
            ConfirmedBookings.VenueTenantId,
            ConcertCommandReceipt.ShareSummaryOperation,
            Guid.NewGuid(),
            fingerprint,
            grant.Id.ToString(),
            DateTime.UnixEpoch);
        return new Recovery(concert, grant, receipt, fingerprint);
    }

    private sealed record Recovery(
        ConcertEntity Concert,
        ConcertShare Grant,
        ConcertCommandReceipt Receipt,
        Sha256Digest Fingerprint);
}
