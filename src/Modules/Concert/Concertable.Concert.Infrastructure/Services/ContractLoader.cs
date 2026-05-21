using Concertable.Contract.Contracts;
using Concertable.Shared.Exceptions;

namespace Concertable.Concert.Infrastructure.Services;

internal sealed class ContractLoader(
    IApplicationRepository applicationRepository,
    IOpportunityRepository opportunityRepository,
    IBookingRepository bookingRepository,
    IConcertRepository concertRepository,
    IContractModule contractModule) : IContractLoader
{
    private enum Kind { Application, Opportunity, Booking, Concert }
    private readonly record struct Key(Kind Kind, int Id);

    private readonly Dictionary<Key, IContract> cache = [];

    public Task<IContract> LoadByApplicationIdAsync(int applicationId) =>
        ResolveAsync(new Key(Kind.Application, applicationId), applicationRepository.GetContractIdByIdAsync);

    public Task<IContract> LoadByOpportunityIdAsync(int opportunityId) =>
        ResolveAsync(new Key(Kind.Opportunity, opportunityId), opportunityRepository.GetContractIdByIdAsync);

    public Task<IContract> LoadByBookingIdAsync(int bookingId) =>
        ResolveAsync(new Key(Kind.Booking, bookingId), bookingRepository.GetContractIdByIdAsync);

    public Task<IContract?> TryLoadByBookingIdAsync(int bookingId) =>
        TryResolveAsync(new Key(Kind.Booking, bookingId), bookingRepository.GetContractIdByIdAsync);

    public Task<IContract> LoadByConcertIdAsync(int concertId) =>
        ResolveAsync(new Key(Kind.Concert, concertId), concertRepository.GetContractIdByIdAsync);

    private async Task<IContract> ResolveAsync(Key key, Func<int, Task<int?>> resolveContractId)
    {
        if (cache.TryGetValue(key, out var hit))
            return hit;

        var contractId = await resolveContractId(key.Id)
            ?? throw new NotFoundException($"{key.Kind} {key.Id} not found");
        var contract = await contractModule.GetByIdAsync(contractId)
            ?? throw new NotFoundException($"No contract with id {contractId}");

        cache[key] = contract;
        return contract;
    }

    private async Task<IContract?> TryResolveAsync(Key key, Func<int, Task<int?>> resolveContractId)
    {
        try { 
            return await ResolveAsync(key, resolveContractId); 
        }
        catch (NotFoundException) { 
            return null; 
        }
    }
}
