using Concertable.B2B.Concert.Contracts.Seed;
using Concertable.Contracts;

namespace Concertable.B2B.Seeding.Fixture;

public sealed partial class B2BSeedFixture
{
    private IReadOnlyList<ConcertSeedSpec>? concerts;

    public IReadOnlyList<ConcertSeedSpec> Concerts => concerts ??= BuildConcerts();

    private IReadOnlyList<ConcertSeedSpec> BuildConcerts() =>
    [
        ConcertSeedSpec.Create(1, "Ultimate Dance Party", 27m, 160, Artists[0], Venues[5], -35, 2, Now),
        ConcertSeedSpec.Create(2, "Boogie Wonderland", 25m, 120, Artists[0], Venues[0], 150, 0, Now, [Genre.Rock, Genre.Indie]),
        ConcertSeedSpec.Create(3, "Funk it up", 20m, 150, Artists[1], Venues[0], 180, 0, Now, [Genre.Rock, Genre.Indie]),
        ConcertSeedSpec.Create(4, "Boogie it up!", 20m, 150, Artists[1], Venues[0], -85, -85, Now),
        ConcertSeedSpec.CreateHire(5, "VenueHire Spectacular", 30m, 200, Artists[0], Venues[0], -40, -40, Now),
        ConcertSeedSpec.Create(6, "Awaiting Show", 15m, 100, Artists[0], Venues[0], 2, 3, Now),
        ConcertSeedSpec.Create(7, "DoorSplit Settlement Show", 20m, 100, Artists[0], Venues[0], -60, -60, Now),
        ConcertSeedSpec.Create(8, "Versus Settlement Show", 20m, 100, Artists[0], Venues[0], -90, -90, Now),
        ConcertSeedSpec.Create(9, "Past Versus Show", 20m, 100, Artists[0], Venues[0], -120, -120, Now),
        ConcertSeedSpec.Create(10, "Past FlatFee Show", 20m, 100, Artists[0], Venues[0], -85, -85, Now),
        ConcertSeedSpec.CreateHire(11, "Past VenueHire Show", 30m, 100, Artists[0], Venues[0], -40, -40, Now),
        ConcertSeedSpec.Create(12, "Past DoorSplit Show", 20m, 100, Artists[0], Venues[0], -60, -60, Now),
        ConcertSeedSpec.Create(13, "Upcoming FlatFee Show", 20m, 150, Artists[1], Venues[0], 15, 0, Now, [Genre.Rock, Genre.Indie]),
        ConcertSeedSpec.CreateHire(14, "Upcoming VenueHire Show", 30m, 200, Artists[0], Venues[0], 20, 0, Now, [Genre.Rock, Genre.Indie]),
        ConcertSeedSpec.Create(15, "Rockin' all Night", 15m, 120, Artists[0], Venues[0], -60, -58, Now),
        ConcertSeedSpec.Create(16, "Non Stop Party", 12m, 110, Artists[1], Venues[0], -60, -55, Now),
        ConcertSeedSpec.Create(17, "Super Mix", 18m, 130, Artists[2], Venues[0], -60, -52, Now),
        ConcertSeedSpec.Create(18, "Hip-Hop till you flip-flop", 10m, 100, Artists[3], Venues[0], -60, -49, Now),
        ConcertSeedSpec.Create(19, "Dance the night away", 25m, 140, Artists[0], Venues[1], -55, -46, Now),
        ConcertSeedSpec.Create(20, "Dizzy One", 20m, 150, Artists[1], Venues[1], -55, -43, Now),
        ConcertSeedSpec.Create(21, "Beers and Boombox", 30m, 170, Artists[4], Venues[1], -55, -40, Now),
        ConcertSeedSpec.Create(22, "Rockin' Tonight!", 16m, 130, Artists[5], Venues[1], -55, -37, Now),
        ConcertSeedSpec.Create(23, "Groovin' All Night", 14m, 115, Artists[0], Venues[2], -50, -34, Now),
        ConcertSeedSpec.Create(24, "Nonstop Vibes", 22m, 135, Artists[1], Venues[2], -50, -31, Now),
        ConcertSeedSpec.Create(25, "Electric Dreams", 13m, 125, Artists[6], Venues[2], -50, -28, Now),
        ConcertSeedSpec.Create(26, "Beat Drop Frenzy", 11m, 120, Artists[7], Venues[2], -50, -25, Now),
        ConcertSeedSpec.Create(27, "Summer Jam", 19m, 140, Artists[0], Venues[3], -45, -22, Now),
        ConcertSeedSpec.Create(28, "Midnight Madness", 17m, 135, Artists[1], Venues[3], -45, -19, Now),
        ConcertSeedSpec.Create(29, "Like a Boss", 21m, 145, Artists[8], Venues[3], -45, -16, Now),
        ConcertSeedSpec.Create(30, "Lights and Sound", 18m, 140, Artists[9], Venues[3], -45, -13, Now),
        ConcertSeedSpec.Create(31, "Rhythm Nation", 26m, 155, Artists[0], Venues[4], -40, -10, Now),
        ConcertSeedSpec.Create(32, "Bass Drop Party", 15m, 120, Artists[1], Venues[4], -40, -7, Now),
        ConcertSeedSpec.Create(33, "Chill & Thrill", 28m, 160, Artists[10], Venues[4], -40, -4, Now),
        ConcertSeedSpec.Create(34, "Vibin' till Night", 24m, 150, Artists[11], Venues[4], -40, -1, Now),
        ConcertSeedSpec.Create(35, "Rock Your Soul", 23m, 130, Artists[1], Venues[5], -35, 5, Now),
        ConcertSeedSpec.Create(36, "Danceaway", 29m, 155, Artists[12], Venues[5], -35, 8, Now),
        ConcertSeedSpec.Create(37, "Bassline Groove Beats", 10m, 110, Artists[13], Venues[5], -35, 11, Now),
        ConcertSeedSpec.Create(38, "Once in a Lifetime!", 15m, 125, Artists[0], Venues[6], -30, 14, Now),
        ConcertSeedSpec.Create(39, "Jungle Fever", 30m, 180, Artists[1], Venues[6], -30, 17, Now),
        ConcertSeedSpec.Create(40, "Boogie Nights", 20m, 100, Artists[0], Venues[3], 5, 6, Now),
        ConcertSeedSpec.Create(41, "Bass in the Air", 30m, 140, Artists[7], Venues[4], 10, 18, Now),
        ConcertSeedSpec.CreateHire(42, "Jumpin and thumpin", 15m, 100, Artists[10], Venues[5], 15, 22, Now),
        ConcertSeedSpec.Create(43, "Groove Night", 18m, 130, Artists[2], Venues[0], 4, -1, Now),
        ConcertSeedSpec.Create(44, "Electric Midnight", 22m, 140, Artists[0], Venues[0], 6, 0, Now),
        ConcertSeedSpec.Create(45, "Summer Haze", 20m, 150, Artists[3], Venues[0], 32, 10, Now),
        ConcertSeedSpec.Create(46, "Night Drive", 25m, 160, Artists[4], Venues[0], 34, 12, Now),
        ConcertSeedSpec.CreateHire(47, "Weekend Rush", 15m, 120, Artists[5], Venues[0], 36, 14, Now),
    ];
}
