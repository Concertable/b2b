using Concertable.DataAccess;
using Concertable.Seeding;
using Concertable.Seeding.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Concertable.Contract.Infrastructure.Data.Seeders;

internal class ContractTestSeeder : ITestSeeder
{
    public int Order => 3;

    private readonly ContractDbContext context;
    private readonly SeedData seed;

    public ContractTestSeeder(ContractDbContext context, SeedData seed)
    {
        this.context = context;
        this.seed = seed;
    }

    public Task MigrateAsync(CancellationToken ct = default) => context.Database.MigrateAsync(ct);

    public async Task SeedAsync(CancellationToken ct = default)
    {
        await context.Contracts.SeedIfEmptyAsync(async () =>
        {
            seed.FlatFeeAppContract = FlatFeeContractEntity.Create(100m, PaymentMethod.Cash);
            seed.ConfirmedAppContract = FlatFeeContractEntity.Create(100m, PaymentMethod.Cash);
            seed.AwaitingPaymentAppContract = FlatFeeContractEntity.Create(100m, PaymentMethod.Cash);
            seed.VersusAppContract = VersusContractEntity.Create(100m, 70m, PaymentMethod.Cash);
            seed.DoorSplitAppContract = DoorSplitContractEntity.Create(70m, PaymentMethod.Cash);
            seed.VenueHireAppContract = VenueHireContractEntity.Create(150m, PaymentMethod.Cash);
            seed.PostedFlatFeeAppContract = FlatFeeContractEntity.Create(100m, PaymentMethod.Cash);
            seed.PostedDoorSplitAppContract = DoorSplitContractEntity.Create(70m, PaymentMethod.Cash);
            seed.PostedVersusAppContract = VersusContractEntity.Create(100m, 70m, PaymentMethod.Cash);
            seed.PostedVenueHireAppContract = VenueHireContractEntity.Create(150m, PaymentMethod.Cash);

            context.Contracts.AddRange(
                seed.FlatFeeAppContract,
                seed.ConfirmedAppContract,
                seed.AwaitingPaymentAppContract,
                seed.VersusAppContract,
                seed.DoorSplitAppContract,
                seed.VenueHireAppContract,
                seed.PostedFlatFeeAppContract,
                seed.PostedDoorSplitAppContract,
                seed.PostedVersusAppContract,
                seed.PostedVenueHireAppContract);

            await context.SaveChangesAsync(ct);
        });
    }
}
