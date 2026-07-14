using Concertable.B2B.Concert.Application.Requests;
using Concertable.B2B.Concert.Domain.Entities;

namespace Concertable.B2B.Concert.Application.Interfaces;

/// <summary>
/// Snapshots the agreed terms into a <see cref="ContractEntity"/> at Accept. Must run
/// inside the accept transition effect so the contract commits atomically with the state change.
/// </summary>
internal interface IContractIssuer
{
    Task IssueAsync(ApplicationEntity application, int bookingId, ESignatureRequest venueESignature);
}
