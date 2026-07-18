namespace Concertable.B2B.Tenant.Application.Tax;

/// <summary>Pure region VAT arithmetic — no registration policy. The interface exists so the region swaps the
/// implementation at startup (real polymorphism, not test mocking). The rate is a constant of the implementation:
/// it changes only by a deliberate code edit (which also needs tax-point transition logic), never a config flip.</summary>
internal interface IVatCalculator
{
    /// <summary>The region's standard VAT rate as a fraction (e.g. 0.20 for 20%).</summary>
    decimal Rate { get; }

    /// <summary>The VAT portion of a VAT-inclusive <paramref name="gross"/>. Pure; applies no registration policy.</summary>
    decimal Calculate(decimal gross);
}
