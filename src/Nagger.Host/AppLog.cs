namespace Nagger.Host;

public static partial class AppLog
{
    [LoggerMessage(EventId = 1002, Level = LogLevel.Information, Message = "Task created {TaskId}")]
    public static partial void TaskCreated(ILogger logger, long taskId);

    [LoggerMessage(EventId = 1004, Level = LogLevel.Information, Message = "Dispatch {MessageType} completed in {ElapsedMs}ms")]
    public static partial void DispatchSucceeded(ILogger logger, string messageType, long elapsedMs);

    [LoggerMessage(EventId = 1005, Level = LogLevel.Warning, Message = "Dispatch {MessageType} rejected by validation ({ErrorType}) in {ElapsedMs}ms")]
    public static partial void DispatchValidationFailed(ILogger logger, string messageType, string errorType, long elapsedMs);

    [LoggerMessage(EventId = 1006, Level = LogLevel.Error, Message = "Dispatch {MessageType} failed ({ErrorType}) in {ElapsedMs}ms")]
    public static partial void DispatchFailed(ILogger logger, string messageType, string errorType, long elapsedMs);
}
