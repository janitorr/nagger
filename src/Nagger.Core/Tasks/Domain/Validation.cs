namespace Nagger.Core.Tasks.Domain;

public sealed class ValidationException(IReadOnlyDictionary<string, string[]> errors)
    : Exception("Validation failed")
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}
