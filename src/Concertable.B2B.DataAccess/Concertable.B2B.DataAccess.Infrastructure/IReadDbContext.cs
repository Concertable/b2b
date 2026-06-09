using Concertable.B2B.Artist.Domain;
using Concertable.B2B.Concert.Domain;
using Concertable.B2B.Contract.Domain;
using Concertable.B2B.User.Domain;
using Concertable.B2B.Conversations.Domain;
using Concertable.B2B.Venue.Domain;

namespace Concertable.B2B.DataAccess.Infrastructure;

public interface IReadDbContext
{
    IQueryable<UserEntity> Users { get; }
    IQueryable<ArtistEntity> Artists { get; }
    IQueryable<VenueEntity> Venues { get; }
    IQueryable<VenueImageEntity> VenueImages { get; }
    IQueryable<ConcertEntity> Concerts { get; }
    IQueryable<ConcertImageEntity> ConcertImages { get; }
    IQueryable<OpportunityEntity> Opportunities { get; }
    IQueryable<ApplicationEntity> Applications { get; }
    IQueryable<BookingEntity> Bookings { get; }
    IQueryable<MessageEntity> Messages { get; }
    IQueryable<ContractEntity> Contracts { get; }
    IQueryable<FlatFeeContractEntity> FlatFeeContracts { get; }
    IQueryable<DoorSplitContractEntity> DoorSplitContracts { get; }
    IQueryable<VersusContractEntity> VersusContracts { get; }
    IQueryable<VenueHireContractEntity> VenueHireContracts { get; }
    IQueryable<ArtistRatingProjection> ArtistRatingProjections { get; }
    IQueryable<VenueRatingProjection> VenueRatingProjections { get; }
    IQueryable<ConcertRatingProjection> ConcertRatingProjections { get; }
}
