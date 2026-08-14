using Dunet;
using Reunion.Errors;

namespace Concertable.B2B.Conversations.Application.Errors;

[Union(EnableImplicitConversions = false)]
internal abstract partial record ModerationError : IError
{
    public ErrorDefinition Definition => this switch
    {
        MessageNotFound => ErrorDefinition.NotFound<MessageNotFound>(),
        ReportNotFound => ErrorDefinition.NotFound<ReportNotFound>(),
        AlreadyResolved => ErrorDefinition.Conflict<AlreadyResolved>("This report has already been resolved.")
    };

    public partial record MessageNotFound;

    public partial record ReportNotFound;

    public partial record AlreadyResolved;
}
