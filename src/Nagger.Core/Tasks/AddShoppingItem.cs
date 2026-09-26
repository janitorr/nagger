using Mediator;
using Nagger.Core.Tasks.Domain;

namespace Nagger.Core.Tasks;

public sealed record AddShoppingItemResult(ShoppingItem Item, bool Created);

public sealed record AddShoppingItemCommand(string? Name) : ICommand<AddShoppingItemResult>
{
    public string ParseName() => ShoppingItemName.Parse(Name);
}

public sealed class AddShoppingItemHandler(IShoppingItemStore store)
    : ICommandHandler<AddShoppingItemCommand, AddShoppingItemResult>
{
    public async ValueTask<AddShoppingItemResult> Handle(
        AddShoppingItemCommand command,
        CancellationToken cancellationToken
    )
    {
        var name = command.ParseName();
        var existing = await store.GetByNameAsync(name, cancellationToken);
        if (existing is not null)
            return new AddShoppingItemResult(existing, false);

        var created = await store.AddAsync(new ShoppingItem(0, name), cancellationToken);
        return new AddShoppingItemResult(created, true);
    }
}
