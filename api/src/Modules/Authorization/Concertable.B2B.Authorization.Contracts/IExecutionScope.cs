namespace Concertable.B2B.Authorization.Contracts;

/// <summary>
/// The trusted execution stance a request-free caller has explicitly established. <see langword="null"/>
/// means interactive or anonymous: the absence of an HTTP request is not itself authority, so an
/// unestablished caller sees nothing rather than everything.
/// </summary>
public interface IExecutionScope
{
    ExecutionPurpose? Purpose { get; }
}

/// <summary>
/// Establishes a trusted execution stance for the duration of the returned lifetime. Workers, seed hosts and
/// outbox handlers enter one at their composition root; disposing it restores the previous stance.
/// </summary>
public interface IExecutionScopeActivator
{
    IDisposable Enter(ExecutionPurpose purpose);

    /// <summary>
    /// Establishes the interactive stance, whatever the caller inherited. A request pipeline enters this so a
    /// process that also runs trusted background work can never serve a request from that work's stance.
    /// </summary>
    IDisposable EnterInteractive();
}
