using System.Security.Cryptography;
using System.Text;
using Concertable.Kernel;

namespace Concertable.B2B.DataAccess.Application;

public abstract class ResourceCommandReceipt : IGuidEntity
{
    protected ResourceCommandReceipt() { }

    public Guid Id { get; protected set; }
    public Guid IssuedByTenantId { get; protected set; }
    public string Operation { get; protected set; } = null!;
    public Guid RequestId { get; protected set; }
    public string PayloadHash { get; protected set; } = null!;
    public string Outcome { get; protected set; } = null!;
    public DateTime RecordedAtUtc { get; protected set; }

    protected void Initialize(
        Guid issuedByTenantId, string operation, Guid requestId, string payloadHash, string outcome, DateTime at)
    {
        Id = Guid.NewGuid();
        IssuedByTenantId = issuedByTenantId;
        Operation = operation;
        RequestId = requestId;
        PayloadHash = payloadHash;
        Outcome = outcome;
        RecordedAtUtc = at;
    }

    public bool Matches(string payloadHash) => PayloadHash == payloadHash;

    public static string HashPayload(params ReadOnlySpan<object?> parts)
    {
        var builder = new StringBuilder();
        foreach (var part in parts)
            builder.Append(part switch
            {
                null => "\u0000",
                DateTime value => value.ToString("O"),
                _ => part.ToString(),
            }).Append('\u001f');

        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString())));
    }
}
