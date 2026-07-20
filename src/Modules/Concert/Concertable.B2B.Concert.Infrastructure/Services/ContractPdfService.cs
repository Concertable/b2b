using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Infrastructure.Pdf;
using Microsoft.Extensions.Logging;

namespace Concertable.B2B.Concert.Infrastructure.Services;

internal sealed class ContractPdfService : IContractPdfService
{
    private readonly IPdfBlobCache pdfCache;
    private readonly ILogger<ContractPdfService> logger;

    public ContractPdfService(IPdfBlobCache pdfCache, ILogger<ContractPdfService> logger)
    {
        this.pdfCache = pdfCache;
        this.logger = logger;
    }

    public Task<byte[]> GetOrCreateAsync(ContractEntity contract, CancellationToken ct = default)
    {
        var blobName = contract.PdfBlobName
            ?? throw new InvalidOperationException("Contract has no assigned PDF blob name");

        // The contract is a pure deterministic function of the immutable snapshot, so rendering on
        // demand is byte-identical to rendering at accept — no reason to pre-generate.
        return pdfCache.GetOrCreateAsync(blobName, new ContractDocument(contract, logger), ct);
    }
}
