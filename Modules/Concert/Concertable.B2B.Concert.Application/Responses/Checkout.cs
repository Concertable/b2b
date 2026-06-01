using System.Text.Json.Serialization;

namespace Concertable.B2B.Concert.Application.Responses;

internal sealed record Checkout(IPaymentAmount Amount, PayeeSummary Payee, CheckoutSession Session, CheckoutLabels Labels);

internal sealed record CheckoutLabels(string SummaryTitle, string SubmitLabel, string? PaymentHint)
{
    internal static readonly CheckoutLabels Charge = new("Summary", "Confirm & Pay", null);

    internal static readonly CheckoutLabels Settlement = new(
        "Settlement",
        "Confirm",
        "Saved card required for settlement after the concert.");
}

internal sealed record PayeeSummary(string Name, string? Email);

[JsonDerivedType(typeof(FlatPayment), "flat")]
[JsonDerivedType(typeof(DoorSharePayment), "doorShare")]
[JsonDerivedType(typeof(GuaranteedDoorPayment), "guaranteedDoor")]
internal interface IPaymentAmount { }

internal sealed record FlatPayment(decimal Amount) : IPaymentAmount;
internal sealed record DoorSharePayment(decimal ArtistPercent) : IPaymentAmount;
internal sealed record GuaranteedDoorPayment(decimal Guarantee, decimal ArtistPercent) : IPaymentAmount;
