using System.Globalization;

namespace Concertable.B2B.Concert.Application.Renderers;

/// <summary>
/// Canonical, representation-independent formatting for the terms fingerprint: a value's meaning,
/// not its storage shape, drives the hash. Decimals are scale-normalised (180 and 180.00 hash the
/// same) and instants are Kind-normalised (a DB-read Unspecified and an in-memory UTC of the same
/// tick count hash the same), so a no-op re-save can never invalidate the artist's recorded consent.
/// </summary>
internal static class TermsFingerprintFormat
{
    public static string Number(decimal value) =>
        value.ToString("0.############################", CultureInfo.InvariantCulture);

    public static string Instant(DateTime value) =>
        DateTime.SpecifyKind(value, DateTimeKind.Utc).ToString("O", CultureInfo.InvariantCulture);
}
