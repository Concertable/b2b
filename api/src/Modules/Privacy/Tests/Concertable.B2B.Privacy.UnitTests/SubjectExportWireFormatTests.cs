using System.Text.Json;
using Concertable.B2B.Booking.Contracts;
using Concertable.B2B.Privacy.Infrastructure.Services;
using Concertable.B2B.Deal.Contracts.Enums;

namespace Concertable.B2B.Privacy.UnitTests;

public sealed class SubjectExportWireFormatTests
{
    private static readonly JsonSerializerOptions SerializerOptions = SubjectExportSerializerOptions.Value;

    [Theory]
    [InlineData(DealType.FlatFee)]
    [InlineData(DealType.DoorSplit)]
    [InlineData(DealType.Versus)]
    [InlineData(DealType.VenueHire)]
    public void DealType_SerializesAsItsName(DealType dealType)
    {
        var contract = new SubjectContractDto
        {
            DealType = dealType,
            CreatedAtUtc = DateTime.UtcNow,
        };

        var json = JsonSerializer.Serialize(contract, SerializerOptions);

        using var document = JsonDocument.Parse(json);
        var written = document.RootElement.GetProperty("dealType");
        Assert.Equal(JsonValueKind.String, written.ValueKind);
        Assert.Equal(dealType.ToString(), written.GetString());
        Assert.False(document.RootElement.TryGetProperty("venueName", out _));
        Assert.False(document.RootElement.TryGetProperty("artistName", out _));
    }
}
