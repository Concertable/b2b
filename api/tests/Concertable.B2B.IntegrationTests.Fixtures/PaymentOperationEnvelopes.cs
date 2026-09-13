using System.Security.Cryptography;
using System.Text;
using Concertable.Payment.Contracts;

namespace Concertable.B2B.IntegrationTests.Fixtures;

internal static class PaymentOperationEnvelopes
{
    public static Dictionary<string, string> Metadata(
        PaymentOperationReference reference,
        Guid? operationId = null)
    {
        var metadata = new Dictionary<string, string>
        {
            [PaymentMetadataKeys.Type] = reference.OperationType,
            [PaymentMetadataKeys.OperationType] = reference.OperationType,
            [PaymentMetadataKeys.ClientReference] = reference.ClientReference
        };
        if (operationId is { } value)
            metadata[PaymentMetadataKeys.OperationId] = value.ToString();

        return metadata;
    }

    // The message id has to be stable per operation: an inbox row is what makes redelivery idempotent, so a
    // fresh id per call would let a repeated simulation apply the same outcome twice.
    public static Guid StableId(PaymentOperationReference reference) =>
        new(MD5.HashData(Encoding.UTF8.GetBytes($"{reference.OperationType}:{reference.ClientReference}")));

    public static Guid StableId(Guid operationId, Type messageType) =>
        new(MD5.HashData(Encoding.UTF8.GetBytes($"{operationId}:{messageType.FullName}")));
}
