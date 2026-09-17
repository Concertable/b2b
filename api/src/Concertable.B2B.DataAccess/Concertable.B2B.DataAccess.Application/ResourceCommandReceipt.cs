using System.Security.Cryptography;
using System.Text;
using Concertable.Kernel;

namespace Concertable.B2B.DataAccess.Application;

/// <summary>
/// What a protected command already decided for one caller-supplied request identity. A retry of the same
/// request returns the recorded outcome instead of acting twice; the same identity carrying a different
/// payload is a conflict, because the caller is no longer asking the question it was answered.
/// <para>
/// Each owning module declares its own receipt table over its own operations: a receipt is only meaningful
/// beside the rows the command wrote, and it commits with them.
/// </para>
/// </summary>
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
