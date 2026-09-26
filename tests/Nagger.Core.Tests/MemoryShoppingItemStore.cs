using Nagger.Core.Tasks;
using Nagger.Core.Tasks.Domain;

namespace Nagger.Core.Tests;

internal sealed class MemoryShoppingItemStore(params ShoppingItem[] items) : IShoppingItemStore
{
    public List<ShoppingItem> Items { get; } = [.. items];
    public int Adds { get; private set; }
    public int Removes { get; private set; }

    public ValueTask<ShoppingItem> AddAsync(ShoppingItem item, CancellationToken cancellationToken)
    {
        item = item with { Id = Items.Count + 1 };
        Items.Add(item);
        Adds++;
        return ValueTask.FromResult(item);
    }

    public ValueTask<ShoppingItem?> GetByNameAsync(string name, CancellationToken cancellationToken) =>
        ValueTask.FromResult(
            Items.SingleOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase))
        );

    public ValueTask RemoveAsync(ShoppingItem item, CancellationToken cancellationToken)
    {
        Items.RemoveAll(x => x.Id == item.Id);
        Removes++;
        return ValueTask.CompletedTask;
    }

    public ValueTask<IReadOnlyList<ShoppingItem>> GetAllAsync(CancellationToken cancellationToken) =>
        ValueTask.FromResult<IReadOnlyList<ShoppingItem>>(Items.OrderBy(x => x.Id).ToList());
}
