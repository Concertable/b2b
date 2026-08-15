using Dunet;
using Reunion.Errors;

namespace Concertable.B2B.Conversations.Application.Errors;

[Union(EnableImplicitConversions = false)]
internal abstract partial record ReportMessageError : IError
{
    public ErrorDefinition Definition => this switch
    {
        MessageNotFound => ErrorDefinition.NotFound<MessageNotFound>()
    };

    public partial record MessageNotFound;
}
