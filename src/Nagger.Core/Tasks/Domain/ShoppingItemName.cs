namespace Nagger.Core.Tasks.Domain;

public static class ShoppingItemName
{
    public const int MaxLength = 200;

    public static string Parse(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ValidationException(new Dictionary<string, string[]> { ["name"] = ["Name is required."] });

        var trimmed = name.Trim();
        if (trimmed.Length > MaxLength)
            throw new ValidationException(
                new Dictionary<string, string[]> { ["name"] = [$"Name must be at most {MaxLength} characters."] }
            );

        return trimmed;
    }
}
