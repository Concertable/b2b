using Concertable.Payment.Contracts;

namespace Concertable.B2B.Infrastructure.Payments;

public static class PaymentMetadataExtensions
{
    extension(IReadOnlyDictionary<string, string> metadata)
    {
        public bool TryGetOperationId(out Guid operationId)
        {
            operationId = Guid.Empty;
            return metadata.TryGetValue(PaymentMetadataKeys.OperationId, out var raw)
                && Guid.TryParse(raw, out operationId);
        }
    }
}
