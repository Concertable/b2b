namespace Concertable.B2B.Authorization.Contracts;

public static class ResourceAccessExtensions
{
    extension(ResourceAccess access)
    {
        public bool Includes(ResourceAccess required) => required != ResourceAccess.None && access >= required;
    }
}
