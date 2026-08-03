using System.Text.Json.Serialization;

namespace Nagger.Host.Api;

public sealed record ValidationError([property: JsonPropertyName("errors")] IReadOnlyDictionary<string, string[]> Errors);
