using System.Net;
using Concertable.B2B.Application.Contracts;
using Concertable.B2B.Application.Domain.Entities;
using Concertable.B2B.Application.Domain.Lifecycle;
using Concertable.B2B.DataAccess.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Concertable.B2B.Application.Infrastructure.Data.Configurations;

internal sealed class ApplicationEntityConfiguration : IEntityTypeConfiguration<ApplicationEntity>
{
    private static readonly ValueConverter<IPAddress, string> IpConverter =
        new(ip => ip.ToString(), text => IPAddress.Parse(text));

    public void Configure(EntityTypeBuilder<ApplicationEntity> builder)
    {
        builder.ToTable(Schema.Tables.Applications, Schema.Name);
        builder.HasConcurrencyVersion();
        builder.Property(application => application.State).IsRequired().IsConcurrencyToken();
        builder.HasOne(application => application.VerifyPayment)
            .WithOne()
            .HasForeignKey<VerifyPaymentEntity>(payment => payment.ApplicationId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(application => application.VerifyPayment).AutoInclude();
        builder.HasIndex(application => application.AcceptanceOperationId)
            .IsUnique()
            .HasFilter("[AcceptanceOperationId] IS NOT NULL");
        builder.HasIndex(application => new { application.OpportunityId, application.ArtistId }).IsUnique();
        builder.HasIndex(application => application.OpportunityId)
            .IsUnique()
            .HasFilter($"[State] = {(int)ApplicationState.Accepted}");
        builder.ComplexProperty(application => application.ArtistESignature, ConfigureSignature);
        builder.HasDiscriminator<string>("Discriminator")
            .HasValue<StandardApplication>(nameof(StandardApplication))
            .HasValue<PrepaidApplication>(nameof(PrepaidApplication));
    }

    private static void ConfigureSignature(ComplexPropertyBuilder<ContractSignature> builder)
    {
        builder.Property(signature => signature.Ip).HasConversion(IpConverter).HasMaxLength(45);
        builder.Property(signature => signature.UserAgent).HasMaxLength(512);
    }
}
