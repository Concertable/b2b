using Concertable.B2B.Tenant.Domain.Entities;
using Concertable.B2B.Tenant.Domain.Enums;
using Concertable.B2B.Tenant.Domain.Events;
using Concertable.Kernel;

namespace Concertable.B2B.Tenant.UnitTests;

public sealed class TenantVerificationEntityTests
{
    private static readonly DateTime SubmittedAt = new(2026, 8, 24, 12, 0, 0, DateTimeKind.Utc);

    private readonly Guid tenantId = Guid.NewGuid();
    private readonly Guid adminSub = Guid.NewGuid();

    #region Submit

    [Fact]
    public void Submit_WithEvidence_CreatesPendingVerificationCarryingTheDocuments()
    {
        var document = Document();

        var verification = TenantVerificationEntity.Submit(this.tenantId, [document], SubmittedAt);

        Assert.NotEqual(Guid.Empty, verification.Id);
        Assert.Equal(this.tenantId, verification.TenantId);
        Assert.Equal(TenantVerificationStatus.Pending, verification.Status);
        Assert.Equal(SubmittedAt, verification.SubmittedAt);
        Assert.Null(verification.RejectionReason);
        Assert.Null(verification.ReviewedByAdminSub);
        Assert.Same(document, Assert.Single(verification.Documents));
    }

    [Fact]
    public void Submit_NoEvidence_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => TenantVerificationEntity.Submit(this.tenantId, [], SubmittedAt));
    }

    [Fact]
    public void Submit_RaisesTenantVerificationChangedDomainEvent()
    {
        var verification = TenantVerificationEntity.Submit(this.tenantId, [Document()], SubmittedAt);

        var raised = Assert.IsType<TenantVerificationChangedDomainEvent>(Assert.Single(verification.DomainEvents));
        Assert.Same(verification, raised.Verification);
    }

    #endregion

    #region Approve

    [Fact]
    public void Approve_PendingVerification_RecordsApproval()
    {
        var verification = Submitted();
        var approvedAt = SubmittedAt.AddDays(1);

        verification.Approve(this.adminSub, approvedAt);

        Assert.Equal(TenantVerificationStatus.Approved, verification.Status);
        Assert.Equal(this.adminSub, verification.ReviewedByAdminSub);
        Assert.Equal(approvedAt, verification.ReviewedAt);
        Assert.Null(verification.RejectionReason);
    }

    [Fact]
    public void Approve_ApprovedVerification_ThrowsDomainException()
    {
        var verification = Submitted();
        verification.Approve(this.adminSub, SubmittedAt.AddDays(1));

        Assert.Throws<DomainException>(() => verification.Approve(this.adminSub, SubmittedAt.AddDays(2)));
    }

    [Fact]
    public void Approve_RejectedVerification_ThrowsDomainException()
    {
        var verification = Rejected();

        Assert.Throws<DomainException>(() => verification.Approve(this.adminSub, SubmittedAt.AddDays(2)));
    }

    #endregion

    #region Reject

    [Fact]
    public void Reject_PendingVerification_RecordsRejection()
    {
        var verification = Submitted();
        var rejectedAt = SubmittedAt.AddDays(1);

        verification.Reject(this.adminSub, "Licence is expired.", rejectedAt);

        Assert.Equal(TenantVerificationStatus.Rejected, verification.Status);
        Assert.Equal(this.adminSub, verification.ReviewedByAdminSub);
        Assert.Equal(rejectedAt, verification.ReviewedAt);
        Assert.Equal("Licence is expired.", verification.RejectionReason);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Reject_NoReason_ThrowsDomainException(string? reason)
    {
        var verification = Submitted();

        Assert.Throws<DomainException>(() => verification.Reject(this.adminSub, reason!, SubmittedAt.AddDays(1)));
    }

    [Fact]
    public void Reject_AlreadyRejectedVerification_ThrowsDomainException()
    {
        var verification = Rejected();

        Assert.Throws<DomainException>(() => verification.Reject(this.adminSub, "Still not enough.", SubmittedAt.AddDays(2)));
    }

    #endregion

    #region Resubmit

    [Fact]
    public void Resubmit_RejectedVerification_ReturnsToPendingAppendingEvidenceAndClearingReview()
    {
        var verification = Rejected();
        var newDocument = Document();
        var resubmittedAt = SubmittedAt.AddDays(3);

        verification.Resubmit([newDocument], resubmittedAt);

        Assert.Equal(TenantVerificationStatus.Pending, verification.Status);
        Assert.Equal(resubmittedAt, verification.SubmittedAt);
        Assert.Null(verification.RejectionReason);
        Assert.Null(verification.ReviewedByAdminSub);
        Assert.Null(verification.ReviewedAt);
        Assert.Equal(2, verification.Documents.Count);
        Assert.Contains(newDocument, verification.Documents);
    }

    [Fact]
    public void Resubmit_PendingVerification_ThrowsDomainException()
    {
        var verification = Submitted();

        Assert.Throws<DomainException>(() => verification.Resubmit([Document()], SubmittedAt.AddDays(1)));
    }

    [Fact]
    public void Resubmit_NoEvidence_ThrowsDomainException()
    {
        var verification = Rejected();

        Assert.Throws<DomainException>(() => verification.Resubmit([], SubmittedAt.AddDays(1)));
    }

    #endregion

    #region Domain events

    [Fact]
    public void ClearDomainEvents_RemovesEveryRaisedEvent()
    {
        var verification = Submitted();
        verification.Approve(this.adminSub, SubmittedAt.AddDays(1));

        verification.ClearDomainEvents();

        Assert.Empty(verification.DomainEvents);
    }

    #endregion

    private TenantVerificationEntity Submitted() =>
        TenantVerificationEntity.Submit(this.tenantId, [Document()], SubmittedAt);

    private TenantVerificationEntity Rejected()
    {
        var verification = Submitted();
        verification.Reject(this.adminSub, "Not enough evidence.", SubmittedAt.AddDays(1));
        return verification;
    }

    private static VerificationDocumentEntity Document() =>
        VerificationDocumentEntity.Create(VerificationDocumentType.Licence, $"verification-evidence/{Guid.NewGuid():N}.pdf", SubmittedAt);
}
