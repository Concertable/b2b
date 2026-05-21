namespace Concertable.Organization.Application.Mappers;

internal static class OrganizationMappers
{
    public static OrganizationDto ToDto(this OrganizationEntity org) =>
        new(org.Id, org.LegalName);
}
