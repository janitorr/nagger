namespace Nagger.Host;

public static partial class AppLog
{
    [LoggerMessage(
        EventId = 1000,
        Level = LogLevel.Information,
        Message = "Request completed {Path} {StatusCode} in {ElapsedMs}ms"
    )]
    public static partial void RequestCompleted(ILogger logger, string path, int statusCode, long elapsedMs);

    [LoggerMessage(EventId = 1001, Level = LogLevel.Warning, Message = "Validation rejected for {Path}")]
    public static partial void ValidationRejected(ILogger logger, string path);

    [LoggerMessage(EventId = 1002, Level = LogLevel.Information, Message = "Task created {TaskId}")]
    public static partial void TaskCreated(ILogger logger, long taskId);

    [LoggerMessage(EventId = 1003, Level = LogLevel.Error, Message = "Unexpected failure for {Path}: {ErrorType}")]
    public static partial void UnexpectedFailure(ILogger logger, string path, string errorType);
}
