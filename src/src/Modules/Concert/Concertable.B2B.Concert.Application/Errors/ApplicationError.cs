using Reunion.Errors;
using Dunet;

namespace Concertable.B2B.Concert.Application.Errors;

[Union(EnableImplicitConversions = false)]
internal abstract partial record ApplicationError : IError
{
    public ErrorDefinition Definition => this switch
    {
        NotFound(var applicationId) =>
            ErrorDefinition.NotFound<NotFound>(
                $"Application {applicationId} was not found.")
    };

    [ErrorCode("application.get.not_found")]
    public partial record NotFound(int ApplicationId);
}
