using Concertable.B2B.Concert.Application.Errors;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.ValueObjects;
using Concertable.B2B.Concert.Infrastructure.Services;
using Concertable.B2B.DataAccess.Application;
using Concertable.Contracts.Enums;

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
            recovery.PayloadHash);

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
            ResourceCommandReceipt.HashPayload("different"));

        Assert.True(result.TryGetError(out var error));
        Assert.IsType<ShareConcertSummaryError.RequestConflict>(error);
    }

    [Fact]
    public void RecoverSummaryShareDuplicate_MatchingReceiptWithoutRecordedGrant_Throws()
    {
        var recovery = CreateRecovery();
        var concert = ConcertEntity.CreateDraft(
            ConfirmedBookings.FlatFee(),
            new ConcertDraft("Concert", "About", [Genre.Rock]),
            DateTime.UnixEpoch);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            ConcertService.RecoverSummaryShareDuplicate(
                concert,
                recovery.Receipt,
                recovery.PayloadHash));

        Assert.Contains(recovery.Receipt.Id.ToString(), exception.Message);
        Assert.Contains(recovery.Grant.Id.ToString(), exception.Message);
    }

    [Fact]
    public void RecoverSummaryShareDuplicate_MissingReceipt_ReturnsAlreadyShared()
    {
        var recovery = CreateRecovery();

        var result = ConcertService.RecoverSummaryShareDuplicate(
            recovery.Concert,
            null,
            recovery.PayloadHash);

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
        var payloadHash = ResourceCommandReceipt.HashPayload(
            concert.Id,
            RecipientTenantId,
            null,
            null);
        var receipt = ConcertCommandReceipt.Record(
            ConfirmedBookings.VenueTenantId,
            ConcertCommandReceipt.ShareSummaryOperation,
            Guid.NewGuid(),
            payloadHash,
            grant.Id.ToString(),
            DateTime.UnixEpoch);
        return new Recovery(concert, grant, receipt, payloadHash);
    }

    private sealed record Recovery(
        ConcertEntity Concert,
        ConcertAccessGrant Grant,
        ConcertCommandReceipt Receipt,
        string PayloadHash);
}
