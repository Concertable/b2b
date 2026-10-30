using Dunet;
using Reunion.Errors;

namespace Concertable.B2B.Conversations.Application.Errors;

[Union(EnableImplicitConversions = false)]
internal abstract partial record CreateConversationError : IError
{
    public ErrorDefinition Definition => this switch
    {
        NotPermitted => ErrorDefinition.Forbidden<NotPermitted>(),
        InvalidParticipants => ErrorDefinition.Invalid<InvalidParticipants>(),
        RequestConflict => ErrorDefinition.Conflict<RequestConflict>()
    };

    public partial record NotPermitted;
    public partial record InvalidParticipants;
    public partial record RequestConflict;
}

[Union(EnableImplicitConversions = false)]
internal abstract partial record ConversationAccessError : IError
{
    public ErrorDefinition Definition => this switch
    {
        NotFound(var id) => ErrorDefinition.NotFound<NotFound>($"Conversation {id} was not found."),
        NotPermitted => ErrorDefinition.Forbidden<NotPermitted>(),
        InvalidSequence => ErrorDefinition.Invalid<InvalidSequence>()
    };

    public partial record NotFound(int ConversationId);
    public partial record NotPermitted;
    public partial record InvalidSequence;
}

[Union(EnableImplicitConversions = false)]
internal abstract partial record SendMessageError : IError
{
    public ErrorDefinition Definition => this switch
    {
        NotFound(var id) => ErrorDefinition.NotFound<NotFound>($"Conversation {id} was not found."),
        NotPermitted => ErrorDefinition.Forbidden<NotPermitted>(),
        InvalidMessage => ErrorDefinition.Invalid<InvalidMessage>(),
        RequestConflict => ErrorDefinition.Conflict<RequestConflict>()
    };

    public partial record NotFound(int ConversationId);
    public partial record NotPermitted;
    public partial record InvalidMessage;
    public partial record RequestConflict;
}

[Union(EnableImplicitConversions = false)]
internal abstract partial record AssignConversationMemberError : IError
{
    public ErrorDefinition Definition => this switch
    {
        NotFound(var id) => ErrorDefinition.NotFound<NotFound>($"Conversation {id} was not found."),
        NotPermitted => ErrorDefinition.Forbidden<NotPermitted>(),
        InvalidMembership => ErrorDefinition.Invalid<InvalidMembership>(),
        AlreadyAssigned => ErrorDefinition.Conflict<AlreadyAssigned>(),
        Superseded(var id) => ErrorDefinition.Conflict<Superseded>($"Conversation {id} changed while this assignment was in flight.")
    };

    public partial record NotFound(int ConversationId);
    public partial record NotPermitted;
    public partial record InvalidMembership;
    public partial record AlreadyAssigned;
    public partial record Superseded(int ConversationId);
}
