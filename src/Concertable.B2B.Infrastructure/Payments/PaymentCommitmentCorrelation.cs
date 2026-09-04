namespace Concertable.B2B.Infrastructure.Payments;

/// <summary>
/// The consumer-correlation half of a Payment operation reference. Only the minting step derives one;
/// every later step reads the copy frozen onto the contract. Both formats are frozen vocabulary — a
/// change to either strands the commitments Payment has already indexed.
/// </summary>
public static class PaymentCommitmentCorrelation
{
    public static string ForOpportunityArtist(int opportunityId, Guid artistTenantId) =>
        $"opp:{opportunityId}:artist:{artistTenantId}";

    public static string ForApplication(int applicationId) => $"app:{applicationId}";
}
