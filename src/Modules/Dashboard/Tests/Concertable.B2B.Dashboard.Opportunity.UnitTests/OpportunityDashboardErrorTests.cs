using Concertable.B2B.Dashboard.Opportunity.Application;
using Reunion.Errors;

namespace Concertable.B2B.Dashboard.Opportunity.UnitTests;

public sealed class OpportunityDashboardErrorTests
{
    public static TheoryData<IError, string> Definitions => new()
    {
        {
            new OpportunityDashboardError.MissingVenue(),
            "You must have a venue account."
        },
        {
            new OpportunityDashboardError.MissingArtist(),
            "You must have an artist account."
        }
    };

    [Theory]
    [MemberData(nameof(Definitions))]
    public void Definition_DeclaredError_IsForbidden(IError error, string message)
    {
        Assert.Equal(message, error.Definition.Message);
        Assert.Equal(ErrorKind.Forbidden, error.Definition.Kind);
    }
}
