using Microsoft.AspNetCore.Diagnostics;
using Nagger.Core.Tasks;
using Nagger.Core.Tasks.Domain;

namespace Nagger.Host.Api.ExceptionHandling;

public sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken cancellationToken
    )
    {
        if (exception is ValidationException validation)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new ValidationError(validation.Errors), cancellationToken);
            AppLog.ValidationRejected(logger, context.Request.Path);
            return true;
        }

        if (exception is TaskNotFoundException)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return true;
        }

        if (exception is RecurringTaskNotFoundException)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return true;
        }

        AppLog.UnexpectedFailure(logger, context.Request.Path, exception.GetType().Name);
        await Results
            .Problem(statusCode: StatusCodes.Status500InternalServerError, title: "An unexpected error occurred.")
            .ExecuteAsync(context);
        return true;
    }
}
