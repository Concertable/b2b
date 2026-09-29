using System.Security.Cryptography;
using System.Text;
using System.Buffers.Binary;
using System.Globalization;

namespace Concertable.B2B.DataAccess.Application;

public static class CommandPayloadHash
{
    public static string Compute(params ReadOnlySpan<object?> parts)
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
