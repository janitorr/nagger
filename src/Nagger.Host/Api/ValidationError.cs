namespace Nagger.Host.Api;

public sealed record ValidationError(IReadOnlyDictionary<string, string[]> Errors);
