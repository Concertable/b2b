using Dunet;
using Reunion.Errors;

namespace Concertable.B2B.Conversations.Application.Errors;

[Union(EnableImplicitConversions = false)]
internal abstract partial record ReportMessageError : IError
{
    public ErrorDefinition Definition => this switch
    {
        MessageNotFound => ErrorDefinition.NotFound<MessageNotFound>(),
        Invalid(var errors) => ErrorDefinition.Validation<Invalid>("The report is invalid.", errors)
    };

    public partial record MessageNotFound;

    public partial record Invalid(ValidationErrors Errors);
}
