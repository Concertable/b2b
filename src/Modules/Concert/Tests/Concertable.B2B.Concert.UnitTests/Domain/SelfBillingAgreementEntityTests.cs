using System.Net;
using System.Reflection;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.ValueObjects;

namespace Concertable.B2B.Concert.UnitTests;

public sealed class SelfBillingAgreementEntityTests
{
    private static readonly InvoiceParty Supplier = new(
        Guid.NewGuid(), "Sally Supplier Ltd", "GB123456789", "1 Road", null, "Town", "AB1 2CD", "United Kingdom");

    private static readonly ESignature Signature = new(
        Guid.NewGuid(), new DateTime(2026, 2, 1, 12, 0, 0, DateTimeKind.Utc),
        IPAddress.Loopback, "agent", "Sally Supplier", null);

    private static SelfBillingAgreementEntity Create(DateTime acceptedAtUtc) =>
        SelfBillingAgreementEntity.Create(
            Supplier.TenantId, Supplier, Signature, "clause", "2026-07", acceptedAtUtc, acceptedAtUtc);

    [Fact]
    public void Create_SetsExpiryTwelveMonthsAfterAcceptance()
    {
        var acceptedAtUtc = new DateTime(2026, 2, 1, 12, 0, 0, DateTimeKind.Utc);

        var agreement = Create(acceptedAtUtc);

        Assert.Equal(acceptedAtUtc, agreement.AcceptedAtUtc);
        Assert.Equal(acceptedAtUtc.AddMonths(12), agreement.ExpiresAtUtc);
    }

    [Fact]
    public void Create_StampsSupplierTenantAndPdfBlobUnderSelfBillingAgreementsPrefix()
    {
        var agreement = Create(new DateTime(2026, 2, 1, 12, 0, 0, DateTimeKind.Utc));

        Assert.Equal(Supplier.TenantId, agreement.TenantId);
        Assert.StartsWith("self-billing-agreements/", agreement.PdfBlobName);
        Assert.EndsWith(".pdf", agreement.PdfBlobName);
    }

    [Fact]
    public void Entity_IsImmutable_OnlyTenantIdHasAPublicSetter()
    {
        foreach (var property in typeof(SelfBillingAgreementEntity).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var setterIsPublic = property.SetMethod is { IsPublic: true };
            if (property.Name == nameof(SelfBillingAgreementEntity.TenantId))
                Assert.True(setterIsPublic, "TenantId must be publicly settable for the tenant interceptor");
            else
                Assert.False(setterIsPublic, $"{property.Name} must not have a public setter");
        }
    }
}
