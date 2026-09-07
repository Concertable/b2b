using Concertable.B2B.Concert.Infrastructure.Emails;
using Mjml.Net;
using Scriban;

namespace Concertable.B2B.Concert.UnitTests.Emails;

public sealed class BookingConfirmationEmailContentTests
{
    [Fact]
    public void Render_ShowsLegalNameOnly_AndHtmlEscapes_WhenTaxComplianceAbsent()
    {
        var content = new BookingConfirmationEmailContent(
            new EmailParty("The Venue", "Bar & Grill <Ltd>", null, null),
            new EmailParty("The Artist", "Artist Legal Name", null, null),
            "Monday 1 January 2035");
        var mjmlSource = Template.Parse(content.Template).Render(content, member => member.Name);

        var html = new MjmlRenderer().Render(mjmlSource).Html;

        Assert.Contains("Bar &amp; Grill &lt;Ltd&gt;", html);
        Assert.DoesNotContain("Bar & Grill <Ltd>", html);
        Assert.Contains("Artist Legal Name", html);
        Assert.DoesNotContain("VAT number", html);
        Assert.DoesNotContain("Seed Way", html);
    }
}
