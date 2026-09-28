using Concertable.B2B.Authorization.Infrastructure.Services;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Concertable.B2B.Web.Exceptions;

internal sealed class MalformedTenantHeaderExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService problemDetailsService;

    public MalformedTenantHeaderExceptionHandler(IProblemDetailsService problemDetailsService)
    {
        this.problemDetailsService = problemDetailsService;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not MalformedTenantHeaderException malformedTenantHeader)
            return false;

        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        await problemDetailsService.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Bad Request",
                Detail = malformedTenantHeader.Message
            }
        });
        return true;
    }
}
