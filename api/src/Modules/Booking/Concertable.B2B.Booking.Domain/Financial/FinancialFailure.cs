namespace Concertable.B2B.Booking.Domain.Financial;

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
