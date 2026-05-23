using Concertable.B2B.Contract.Domain.Entities;
using Concertable.DataAccess;
using Concertable.Seeding;
using Concertable.Seeding.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Contract.Infrastructure.Data.Seeders;

internal class ContractDevSeeder : IDevSeeder
{
    public int Order => 3;

    private readonly ContractDbContext context;
    private readonly SeedData seed;

    public ContractDevSeeder(ContractDbContext context, SeedData seed)
    {
        this.context = context;
        this.seed = seed;
    }

    public Task MigrateAsync(CancellationToken ct = default) => context.Database.MigrateAsync(ct);

    public async Task SeedAsync(CancellationToken ct = default)
    {
        await context.Contracts.SeedIfEmptyAsync(async () =>
        {
            seed.ConfirmedAppContract = FlatFeeContractEntity.Create(200m, PaymentMethod.Cash);
            seed.PostedVenueHireAppContract = VenueHireContractEntity.Create(300m, PaymentMethod.Cash);
            seed.PostedFlatFeeAppContract = FlatFeeContractEntity.Create(200m, PaymentMethod.Cash);
            seed.AwaitingPaymentAppContract = FlatFeeContractEntity.Create(150m, PaymentMethod.Cash);
            seed.DoorSplitAppContract = DoorSplitContractEntity.Create(70m, PaymentMethod.Cash);
            seed.VersusAppContract = VersusContractEntity.Create(100m, 70m, PaymentMethod.Cash);
            seed.PostedDoorSplitAppContract = DoorSplitContractEntity.Create(65m, PaymentMethod.Cash);
            seed.PostedVersusAppContract = VersusContractEntity.Create(120m, 60m, PaymentMethod.Cash);
            seed.FlatFeeAppContract = FlatFeeContractEntity.Create(150m, PaymentMethod.Cash);
            seed.VenueHireAppContract = VenueHireContractEntity.Create(300m, PaymentMethod.Cash);
            seed.PastVersusAppContract = VersusContractEntity.Create(100m, 70m, PaymentMethod.Cash);
            seed.PastFlatFeeAppContract = FlatFeeContractEntity.Create(200m, PaymentMethod.Cash);
            seed.PastVenueHireAppContract = VenueHireContractEntity.Create(300m, PaymentMethod.Cash);
            seed.PastDoorSplitAppContract = DoorSplitContractEntity.Create(70m, PaymentMethod.Cash);

            seed.Contracts =
            [
                FlatFeeContractEntity.Create(150m, PaymentMethod.Cash),         // 1  — opp[0] past concert
                FlatFeeContractEntity.Create(120m, PaymentMethod.Cash),         // 2  — opp[1] past concert
                DoorSplitContractEntity.Create(60m, PaymentMethod.Cash),        // 3  — opp[2] past concert
                VersusContractEntity.Create(80m, 50m, PaymentMethod.Cash),      // 4  — opp[3] past concert
                FlatFeeContractEntity.Create(180m, PaymentMethod.Cash),         // 5  — opp[4] past concert
                seed.ConfirmedAppContract,                                      // 6  — opp[5] ConfirmedApp
                FlatFeeContractEntity.Create(160m, PaymentMethod.Cash),         // 7  — opp[6]
                FlatFeeContractEntity.Create(140m, PaymentMethod.Cash),         // 8  — opp[7]
                DoorSplitContractEntity.Create(70m, PaymentMethod.Cash),        // 9  — opp[8]
                VenueHireContractEntity.Create(250m, PaymentMethod.Cash),       // 10 — opp[9]
                FlatFeeContractEntity.Create(170m, PaymentMethod.Cash),         // 11 — opp[10]
                VersusContractEntity.Create(100m, 60m, PaymentMethod.Cash),     // 12 — opp[11]
                FlatFeeContractEntity.Create(150m, PaymentMethod.Cash),         // 13 — opp[12]
                DoorSplitContractEntity.Create(65m, PaymentMethod.Cash),        // 14 — opp[13]
                FlatFeeContractEntity.Create(190m, PaymentMethod.Cash),         // 15 — opp[14]
                VenueHireContractEntity.Create(220m, PaymentMethod.Cash),       // 16 — opp[15]
                FlatFeeContractEntity.Create(155m, PaymentMethod.Cash),         // 17 — opp[16]
                VersusContractEntity.Create(90m, 55m, PaymentMethod.Cash),      // 18 — opp[17]
                DoorSplitContractEntity.Create(60m, PaymentMethod.Cash),        // 19 — opp[18]
                FlatFeeContractEntity.Create(165m, PaymentMethod.Cash),         // 20 — opp[19]
                seed.PostedVenueHireAppContract,                                // 21 — opp[20] PostedVenueHire
                FlatFeeContractEntity.Create(175m, PaymentMethod.Cash),         // 22 — opp[21]
                DoorSplitContractEntity.Create(70m, PaymentMethod.Cash),        // 23 — opp[22]
                VersusContractEntity.Create(110m, 60m, PaymentMethod.Cash),     // 24 — opp[23]
                FlatFeeContractEntity.Create(185m, PaymentMethod.Cash),         // 25 — opp[24]
                FlatFeeContractEntity.Create(195m, PaymentMethod.Cash),         // 26 — opp[25]
                DoorSplitContractEntity.Create(65m, PaymentMethod.Cash),        // 27 — opp[26]
                VenueHireContractEntity.Create(280m, PaymentMethod.Cash),       // 28 — opp[27]
                VersusContractEntity.Create(95m, 55m, PaymentMethod.Cash),      // 29 — opp[28]
                FlatFeeContractEntity.Create(160m, PaymentMethod.Cash),         // 30 — opp[29]
                seed.PostedFlatFeeAppContract,                                  // 31 — opp[30] PostedFlatFee
                FlatFeeContractEntity.Create(140m, PaymentMethod.Cash),         // 32 — opp[31]
                seed.AwaitingPaymentAppContract,                                // 33 — opp[32] AwaitingPayment
                DoorSplitContractEntity.Create(70m, PaymentMethod.Cash),        // 34 — opp[33]
                VersusContractEntity.Create(100m, 60m, PaymentMethod.Cash),     // 35 — opp[34]
                FlatFeeContractEntity.Create(170m, PaymentMethod.Cash),         // 36 — opp[35]
                VenueHireContractEntity.Create(240m, PaymentMethod.Cash),       // 37 — opp[36]
                DoorSplitContractEntity.Create(60m, PaymentMethod.Cash),        // 38 — opp[37]
                FlatFeeContractEntity.Create(180m, PaymentMethod.Cash),         // 39 — opp[38]
                VersusContractEntity.Create(120m, 65m, PaymentMethod.Cash),     // 40 — opp[39]
                FlatFeeContractEntity.Create(155m, PaymentMethod.Cash),         // 41 — opp[40]
                DoorSplitContractEntity.Create(70m, PaymentMethod.Cash),        // 42 — opp[41]
                VenueHireContractEntity.Create(260m, PaymentMethod.Cash),       // 43 — opp[42]
                FlatFeeContractEntity.Create(190m, PaymentMethod.Cash),         // 44 — opp[43]
                VersusContractEntity.Create(105m, 55m, PaymentMethod.Cash),     // 45 — opp[44]
                FlatFeeContractEntity.Create(165m, PaymentMethod.Cash),         // 46 — opp[45]
                DoorSplitContractEntity.Create(65m, PaymentMethod.Cash),        // 47 — opp[46]
                VenueHireContractEntity.Create(290m, PaymentMethod.Cash),       // 48 — opp[47]
                VersusContractEntity.Create(85m, 50m, PaymentMethod.Cash),      // 49 — opp[48]
                seed.DoorSplitAppContract,                                      // 50 — opp[49] FinishedDoorSplit (E2E expects 70%)
                seed.VersusAppContract,                                         // 51 — opp[50] FinishedVersus  (E2E expects £100 + 70%)
                VenueHireContractEntity.Create(170m, PaymentMethod.Cash),       // 52 — opp[51] VenueHireApp opp
                seed.PostedDoorSplitAppContract,                                // 53 — opp[52] PostedDoorSplit
                seed.PostedVersusAppContract,                                   // 54 — opp[53] PostedVersus
                FlatFeeContractEntity.Create(180m, PaymentMethod.Cash),         // 55 — opp[54]
                DoorSplitContractEntity.Create(70m, PaymentMethod.Cash),        // 56 — opp[55]
                VersusContractEntity.Create(110m, 65m, PaymentMethod.Cash),     // 57 — opp[56]
                seed.FlatFeeAppContract,                                        // 58 — opp[57] UpcomingFlatFee
                seed.VenueHireAppContract,                                      // 59 — opp[58] UpcomingVenueHire

                FlatFeeContractEntity.Create(200m, PaymentMethod.Cash),         // 60 — opp[59]
                DoorSplitContractEntity.Create(70m, PaymentMethod.Cash),        // 61 — opp[60]
                VersusContractEntity.Create(100m, 60m, PaymentMethod.Cash),     // 62 — opp[61]
                VenueHireContractEntity.Create(250m, PaymentMethod.Cash),       // 63 — opp[62]
                seed.PastVersusAppContract,                                     // 64 — opp[63] PastVersus (E2E expects £100 + 70%)
                seed.PastFlatFeeAppContract,                                    // 65 — opp[64] PastFlatFee
                seed.PastVenueHireAppContract,                                  // 66 — opp[65] PastVenueHire
                seed.PastDoorSplitAppContract,                                  // 67 — opp[66] PastDoorSplit (E2E expects 70%)
            ];

            context.Contracts.AddRange(seed.Contracts);
            await context.SaveChangesAsync(ct);
        });
    }
}
