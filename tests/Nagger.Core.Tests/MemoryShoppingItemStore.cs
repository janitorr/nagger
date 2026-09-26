using Nagger.Core.Tasks;
using Nagger.Core.Tasks.Domain;

namespace Nagger.Core.Tests;

internal sealed class MemoryShoppingItemStore(params ShoppingItem[] items) : IShoppingItemStore
{
    public List<ShoppingItem> Items { get; } = [.. items];
    public int Adds { get; private set; }
    public int Removes { get; private set; }

    public ValueTask<(ShoppingItem Item, bool Created)> AddIfAbsentAsync(
        ShoppingItem item,
        CancellationToken cancellationToken
    )
    {
        var existing = Find(item.Name);
        if (existing is not null)
            return ValueTask.FromResult<(ShoppingItem, bool)>((existing, false));

        var created = item with { Id = Items.Count + 1 };
        Items.Add(created);
        Adds++;
        return ValueTask.FromResult<(ShoppingItem, bool)>((created, true));
    }

    public ValueTask RemoveByNameAsync(string name, CancellationToken cancellationToken)
    {
        var index = Items.FindIndex(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
        if (index >= 0)
        {
            Items.RemoveAt(index);
            Removes++;
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask<IReadOnlyList<ShoppingItem>> GetAllAsync(CancellationToken cancellationToken) =>
        ValueTask.FromResult<IReadOnlyList<ShoppingItem>>(Items.OrderBy(x => x.Id).ToList());

    private ShoppingItem? Find(string name) =>
        Items.SingleOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
}
