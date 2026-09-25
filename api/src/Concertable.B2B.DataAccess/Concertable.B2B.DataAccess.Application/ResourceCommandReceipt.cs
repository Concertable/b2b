using System.Security.Cryptography;
using System.Text;
using System.Buffers.Binary;
using System.Globalization;
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
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        Span<byte> length = stackalloc byte[sizeof(int)];
        foreach (var part in parts)
        {
            if (part is null)
            {
                BinaryPrimitives.WriteInt32BigEndian(length, -1);
                hash.AppendData(length);
                continue;
            }

            var value = part switch
            {
                DateTime dateTime => dateTime.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
                IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture) ?? string.Empty,
                _ => part.ToString() ?? string.Empty,
            };
            var bytes = Encoding.UTF8.GetBytes(value);
            BinaryPrimitives.WriteInt32BigEndian(length, bytes.Length);
            hash.AppendData(length);
            hash.AppendData(bytes);
        }

        return Convert.ToHexStringLower(hash.GetHashAndReset());
    }
}
