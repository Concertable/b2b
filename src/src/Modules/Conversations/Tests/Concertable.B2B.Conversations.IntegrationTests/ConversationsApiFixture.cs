namespace Concertable.B2B.Conversations.IntegrationTests;

/// <summary>The Conversations integration collection fixture — assertions run through the HTTP API,
/// so no module read context is exposed (the base <see cref="ApiFixture"/> is sufficient).</summary>
public sealed class ConversationsApiFixture : ApiFixture;
