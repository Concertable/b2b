using System.Security.Cryptography;
using System.Text;

namespace Concertable.B2B.Authorization.Contracts;

public static class SystemPresetIds
{
    public static Guid For(Guid tenantId, string presetKey)
    {
        if (tenantId == Guid.Empty || !AuthorizationCatalog.Presets.ContainsKey(presetKey))
            throw new ArgumentException("Unknown tenant or system preset.");
        var input = tenantId.ToByteArray().Concat(Encoding.UTF8.GetBytes(presetKey)).ToArray();
        var hash = SHA256.HashData(input);
        return new Guid(hash.AsSpan(0, 16));
    }
}
