using System.Text.Json.Serialization;

namespace Concertable.B2B.Conversations.Application.DTOs;

internal sealed record MessageDto
{
    public int Id { get; init; }
    public required Guid CounterpartTenantId { get; init; }
    public required MessageSender Sender { get; init; }
    public MessageAction? Action { get; init; }
    public required string Content { get; init; }
}

[JsonConverter(typeof(JsonStringEnumConverter<MessageSenderKind>))]
internal enum MessageSenderKind
{
    [JsonStringEnumMemberName("org")]
    Org,
    [JsonStringEnumMemberName("member")]
    Member
}

/// <summary>Who a message is attributed to, chosen server-side from the active tenant: the counterparty
/// <see cref="MessageSenderKind.Org"/> (brand name + location) for inbound messages, or the
/// <see cref="MessageSenderKind.Member"/> who sent it (email) for your own tenant's outbound.</summary>
internal sealed record MessageSender
{
    public required MessageSenderKind Kind { get; init; }
    public required string DisplayName { get; init; }
    public string? County { get; init; }
    public string? Town { get; init; }

    public static MessageSender Org(string name, string? county, string? town) =>
        new() { Kind = MessageSenderKind.Org, DisplayName = name, County = county, Town = town };

    public static MessageSender Member(string email) =>
        new() { Kind = MessageSenderKind.Member, DisplayName = email };
}
