namespace Concertable.B2B.Concert.Infrastructure;

public static class Schema
{
    public const string Name = "concert";

    public static class Tables
    {
        public const string Concerts = "Concerts";
        public const string ConcertImages = "ConcertImages";
        public const string ConcertRatingProjections = "ConcertRatingProjections";
        public const string Bookings = "Bookings";
        public const string Opportunities = "Opportunities";
        public const string Applications = "Applications";
        public const string ArtistReadModels = "ArtistReadModels";
        public const string ArtistReadModelGenres = "ArtistReadModelGenres";
        public const string VenueReadModels = "VenueReadModels";
    }
}
