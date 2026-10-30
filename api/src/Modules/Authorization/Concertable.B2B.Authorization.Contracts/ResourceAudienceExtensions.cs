namespace Concertable.B2B.Authorization.Contracts;

public static class ResourceAudienceExtensions
{
    extension(ResourceAudience audience)
    {
        public bool IsGranted => audience is ResourceAudience.AssignedResources or ResourceAudience.TenantResources;

        public bool Covers(ResourceAudience required) =>
            audience.IsGranted
            && required.IsGranted
            && (required == ResourceAudience.AssignedResources || audience == ResourceAudience.TenantResources);
    }
}
