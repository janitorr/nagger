using Microsoft.AspNetCore.Diagnostics;
using Nagger.Core.Tasks;
using Nagger.Core.Tasks.Domain;

namespace Nagger.Host.Api.ExceptionHandling;

public sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken
    )
    {
        if (exception is ValidationException validation)
        {
            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            await httpContext.Response.WriteAsJsonAsync(new ValidationError(validation.Errors), cancellationToken);
            AppLog.ValidationRejected(logger, httpContext.Request.Path);
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

        AppLog.UnexpectedFailure(logger, httpContext.Request.Path, exception.GetType().Name);
        await Results
            .Problem(statusCode: StatusCodes.Status500InternalServerError, title: "An unexpected error occurred.")
            .ExecuteAsync(httpContext);
        return true;
    }
}
