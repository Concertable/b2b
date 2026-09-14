namespace Concertable.B2B.Concert.Domain.ValueObjects;

internal sealed record FinancialFailure
{
    public FinancialFailure(string code, string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        Code = code;
        Message = message;
    }

    public string Code { get; }
    public string Message { get; }
}
