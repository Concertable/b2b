namespace Concertable.B2B.Concert.Application.Requests;

/// <summary>
/// The client-supplied half of an e-signature — the typed full name and optional drawn image.
/// Its presence <em>is</em> the consent. The executor combines it with server-owned ambient context
/// (user id, time, IP, user agent) to build the domain <c>ESignature</c>; identity and time are
/// never trusted from the client.
/// </summary>
internal sealed record ESignatureRequest
{
    public required string SignatoryName { get; init; }
    public string? DrawnSignatureImage { get; init; }
}
