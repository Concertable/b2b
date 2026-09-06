namespace Concertable.B2B.Concert.Application.Requests;

internal sealed record ESignatureRequest
{
    public required string SignatoryName { get; init; }
    public string? DrawnSignatureImage { get; init; }
}
