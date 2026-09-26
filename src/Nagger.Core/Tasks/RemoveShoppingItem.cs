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
        await store.RemoveByNameAsync(name, cancellationToken);
        return name;
    }
}
