using System.Security.Cryptography;
using System.Text;
using System.Buffers.Binary;
using System.Globalization;

namespace Concertable.B2B.DataAccess.Application;

public readonly record struct IdempotencyHash
{
    private IdempotencyHash(string value)
    {
        this.Value = value;
    }

    public string Value { get; }

    public static IdempotencyHash Create(params ReadOnlySpan<object?> parts)
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

        return new IdempotencyHash(Convert.ToHexStringLower(hash.GetHashAndReset()));
    }

    public static IdempotencyHash From(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (value.Length != 64 || value.Any(static character =>
                character is not (>= '0' and <= '9' or >= 'a' and <= 'f' or >= 'A' and <= 'F')))
            throw new ArgumentException("An idempotency hash must contain exactly 64 hexadecimal characters.", nameof(value));

        return new IdempotencyHash(value.ToLowerInvariant());
    }
}
