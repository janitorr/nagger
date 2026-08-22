using Microsoft.AspNetCore.Diagnostics;
using Nagger.Core.Tasks;
using Nagger.Core.Tasks.Domain;

namespace Nagger.Host.Api.ExceptionHandling;

public sealed class ApiExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken
    )
    {
        if (exception is ValidationException validation)
        {
            await Results
                .ValidationProblem(validation.Errors.ToDictionary(error => error.Key, error => error.Value))
                .ExecuteAsync(httpContext);
            return true;
        }

        if (exception is TaskNotFoundException)
        {
            httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
            return true;
        }

        if (exception is RecurringTaskNotFoundException)
        {
            httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
            return true;
        }

        await Results
            .Problem(statusCode: StatusCodes.Status500InternalServerError, title: "An unexpected error occurred.")
            .ExecuteAsync(httpContext);
        return true;
    }
}
