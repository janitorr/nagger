using Mediator;
using Nagger.Core.Tasks.Domain;

namespace Nagger.Core.Tasks;

public sealed record RemoveShoppingItemCommand(string? Name) : ICommand<string>
{
    public string ParseName() => ShoppingItemName.Parse(Name);
}

public sealed class RemoveShoppingItemHandler(IShoppingItemStore store)
    : ICommandHandler<RemoveShoppingItemCommand, string>
{
    public async ValueTask<string> Handle(RemoveShoppingItemCommand command, CancellationToken cancellationToken)
    {
        var name = command.ParseName();
        var existing = await store.GetByNameAsync(name, cancellationToken);
        if (existing is not null)
            await store.RemoveAsync(existing, cancellationToken);

        return name;
    }
}
