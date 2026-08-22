using System.Diagnostics;
using Mediator;
using Nagger.Core.Tasks;
using Nagger.Core.Tasks.Domain;

namespace Nagger.Host.Composition.Mediator;

public sealed class DispatchLoggingBehavior<TMessage, TResponse>(
    ILogger<DispatchLoggingBehavior<TMessage, TResponse>> logger
) : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IMessage
{
    public async ValueTask<TResponse> Handle(
        TMessage message,
        MessageHandlerDelegate<TMessage, TResponse> next,
        CancellationToken cancellationToken
    )
    {
        var timer = Stopwatch.StartNew();
        try
        {
            var response = await next(message, cancellationToken);
            timer.Stop();
            AppLog.DispatchSucceeded(logger, message.GetType().Name, timer.ElapsedMilliseconds);
            return response;
        }
        catch (ValidationException)
        {
            timer.Stop();
            AppLog.DispatchValidationFailed(
                logger,
                message.GetType().Name,
                nameof(ValidationException),
                timer.ElapsedMilliseconds
            );
            throw;
        }
        catch (TaskNotFoundException)
        {
            timer.Stop();
            AppLog.DispatchNotFound(
                logger,
                message.GetType().Name,
                nameof(TaskNotFoundException),
                timer.ElapsedMilliseconds
            );
            throw;
        }
        catch (RecurringTaskNotFoundException)
        {
            timer.Stop();
            AppLog.DispatchNotFound(
                logger,
                message.GetType().Name,
                nameof(RecurringTaskNotFoundException),
                timer.ElapsedMilliseconds
            );
            throw;
        }
        catch (Exception exception)
        {
            timer.Stop();
            AppLog.DispatchFailed(logger, message.GetType().Name, exception.GetType().Name, timer.ElapsedMilliseconds);
            throw;
        }
    }
}
