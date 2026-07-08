using System.Globalization;

namespace Concertable.B2B.Concert.Application.Renderers;

internal static class AgreementTermsFormat
{
    private static readonly CultureInfo Gb = CultureInfo.GetCultureInfo("en-GB");

    public static string Gbp(decimal amount) => amount.ToString("C", Gb);

    public static string Percent(decimal percent) => $"{percent.ToString("0.##", Gb)}%";
}
