namespace Concertable.B2B.Authorization.Contracts;

public static class ResourceCoverageExtensions
{
    extension(ResourceCoverage coverage)
    {
        public bool IsGranted => coverage is ResourceCoverage.AssignedResources or ResourceCoverage.TenantResources;

        public bool Covers(ResourceCoverage required) =>
            coverage.IsGranted
            && required.IsGranted
            && (required == ResourceCoverage.AssignedResources || coverage == ResourceCoverage.TenantResources);
    }
}
