using System.Net;
using Concertable.B2B.Concert.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Concertable.B2B.Concert.Infrastructure.Data.Configurations;

/// <summary>
/// Shared owned-type mapping for the <see cref="ESignature"/> value object (one definition for every
/// place it is owned). The IP is persisted through its canonical <see cref="IPAddress"/> text, so the
/// stored value can only ever be a real address; the client-supplied evidence columns are length-bounded.
/// </summary>
internal static class ESignatureConfiguration
{
    private static readonly ValueConverter<IPAddress?, string?> IpConverter =
        new(ip => ip == null ? null : ip.ToString(), text => text == null ? null : IPAddress.Parse(text));

    public static void Configure<TOwner>(OwnedNavigationBuilder<TOwner, ESignature> builder)
        where TOwner : class
    {
        builder.Property(s => s.Ip).HasConversion(IpConverter).HasMaxLength(45);
        builder.Property(s => s.UserAgent).HasMaxLength(512);
    }
}
