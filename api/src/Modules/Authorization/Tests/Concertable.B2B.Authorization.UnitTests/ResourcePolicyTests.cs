namespace Concertable.B2B.Authorization.UnitTests;

public sealed class ResourcePolicyTests
{
    [Theory]
    [InlineData(ResourcePolicy.Membership, "membership")]
    [InlineData(ResourcePolicy.ApplicationGrant, "application_grant")]
    [InlineData(ResourcePolicy.PrincipalAdministration, "principal_administration")]
    public void WireName_Policy_IsSnakeCase(ResourcePolicy policy, string expected)
    {
        Assert.Equal(expected, policy.WireName);
    }
}
