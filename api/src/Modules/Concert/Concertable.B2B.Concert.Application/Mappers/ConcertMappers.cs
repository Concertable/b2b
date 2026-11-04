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

    extension(ConcertSummaryShareError error)
    {
        public ShareConcertSummaryError ToShareConcertSummaryError() => error switch
        {
            ConcertSummaryShareError.NotPermitted =>
                new ShareConcertSummaryError.NotPermitted(),
            ConcertSummaryShareError.InvalidValidity =>
                new ShareConcertSummaryError.InvalidValidity(),
            ConcertSummaryShareError.AlreadyShared =>
                new ShareConcertSummaryError.AlreadyShared()
        };
    }

    extension(ConcertSummaryShareRevocationError error)
    {
        public RevokeConcertSummaryShareError ToRevokeConcertSummaryShareError() => error switch
        {
            ConcertSummaryShareRevocationError.GrantNotFound =>
                new RevokeConcertSummaryShareError.GrantNotFound(),
            ConcertSummaryShareRevocationError.NotTheIssuer =>
                new RevokeConcertSummaryShareError.NotTheIssuer()
        };
    }

    extension(ConcertMemberAssignmentError error)
    {
        public AssignConcertMemberError ToAssignConcertMemberError() => error switch
        {
            ConcertMemberAssignmentError.NotPermitted =>
                new AssignConcertMemberError.NotPermitted(),
            ConcertMemberAssignmentError.AlreadyAssigned =>
                new AssignConcertMemberError.AlreadyAssigned(),
            ConcertMemberAssignmentError.NotAssigned =>
                new AssignConcertMemberError.NotAssigned()
        };
    }

    extension(ConcertShare share)
    {
        public ConcertSummaryShare ToSummaryShare(long accessVersion) => new()
        {
            GrantId = share.Id,
            GrantVersion = 1,
            AccessVersion = accessVersion,
            RecipientTenantId = share.RecipientTenantId,
            RecipientMembershipId = share.RecipientMembershipId,
            ValidFrom = share.SharedAt,
            ValidUntil = share.ExpiresAt
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
