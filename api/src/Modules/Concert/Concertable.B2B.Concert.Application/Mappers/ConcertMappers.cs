using Concertable.B2B.Concert.Application.Responses;
using Concertable.B2B.Concert.Application.DTOs;
using Concertable.B2B.Concert.Application.Errors;
using Concertable.B2B.Concert.Domain.Errors;
using Concertable.B2B.Concert.Domain.Entities;

namespace Concertable.B2B.Concert.Application.Mappers;

internal static class ConcertMappers
{
    extension(ConcertEntity concert)
    {
        public ConcertDto ToDto() => new()
        {
            Id = concert.Id,
            Name = concert.Name,
            ImageUrl = concert.Artist.Avatar,
            StartDate = concert.Period.Start,
            EndDate = concert.Period.End,
            County = concert.Venue.Address.County,
            Town = concert.Venue.Address.Town,
            DatePosted = concert.DatePosted
        };
    }

    extension(ConcertShareError error)
    {
        public ShareConcertError ToShareConcertError() => error switch
        {
            ConcertShareError.ScopeNotShareable(var scope) =>
                new ShareConcertError.ScopeNotShareable(scope),
            ConcertShareError.NotAPrincipal =>
                new ShareConcertError.NotAPrincipal(),
            ConcertShareError.AlreadyShared =>
                new ShareConcertError.AlreadyShared()
        };
    }

    extension(ShareRevocationError error)
    {
        public RevokeConcertShareError ToRevokeConcertShareError() => error switch
        {
            ShareRevocationError.GrantNotFound =>
                new RevokeConcertShareError.GrantNotFound(),
            ShareRevocationError.NotAShare =>
                new RevokeConcertShareError.NotAShare(),
            ShareRevocationError.NotTheIssuer =>
                new RevokeConcertShareError.NotTheIssuer()
        };
    }

    extension(ConcertAccessGrant grant)
    {
        public ConcertShareResponse ToShareResponse() => new()
        {
            GrantId = grant.Id,
            ToTenantId = grant.TenantId,
            ToMemberUserId = grant.MemberUserId,
            Scope = grant.Scope,
            ValidFrom = grant.ValidFrom,
            ValidUntil = grant.ValidUntil
        };
    }

    extension(DoorRevenueDeclarationError error)
    {
        public DeclareDoorRevenueError ToDeclareDoorRevenueError() => error switch
        {
            DoorRevenueDeclarationError.NegativeRevenue =>
                new DeclareDoorRevenueError.Negative()
        };
    }
}
