namespace Concertable.B2B.Organization.Application.Mappers;

internal static class OrganizationMappers
{
    public static OrganizationDto ToDto(this OrganizationEntity org) =>
        new(org.Id, org.LegalName);
}
