using System.Net;
using Concertable.B2B.Concert.Domain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Concertable.B2B.Concert.Infrastructure.Data.Configurations;

/// <summary>
/// Shared mapping for the <see cref="ESignature"/> value object (one definition for every place it is
/// stored). It is a complex type — a value with no identity — so it is stored inline in each owner's
/// row and copied by value; the same signature can sit on an application and its contract without EF
/// tracking one instance under two owners. The IP is persisted through its canonical
/// <see cref="IPAddress"/> text; the client-supplied evidence columns are length-bounded.
/// </summary>
internal static class ESignatureConfiguration
{
    private static readonly ValueConverter<IPAddress, string> IpConverter =
        new(ip => ip.ToString(), text => IPAddress.Parse(text));

    public static void Configure(ComplexPropertyBuilder<ESignature> builder)
    {
        builder.Property(s => s.Ip).HasConversion(IpConverter).HasMaxLength(45);
        builder.Property(s => s.UserAgent).HasMaxLength(512);
    }
}
