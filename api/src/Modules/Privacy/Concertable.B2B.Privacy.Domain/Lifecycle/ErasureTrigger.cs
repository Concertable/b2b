namespace Concertable.B2B.Privacy.Domain.Lifecycle;

/// <summary>The inputs that drive a <see cref="SubjectErasureRequestEntity"/> between <see cref="ErasureState"/>s.</summary>
public enum ErasureTrigger
{
    Begin,
    Defer,
    Complete,
    Fail,
}
