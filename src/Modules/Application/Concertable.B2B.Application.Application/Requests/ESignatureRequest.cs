namespace Concertable.B2B.Application.Application.Requests;

internal sealed record ESignatureRequest
{
    public required string SignatoryName { get; init; }
    public string? DrawnSignatureImage { get; init; }
}
