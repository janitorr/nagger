namespace Nagger.Core.Tasks.Domain;

public static class ShoppingItemName
{
    public static string Parse(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ValidationException(new Dictionary<string, string[]> { ["name"] = ["Name is required."] });

        return name.Trim();
    }
}
