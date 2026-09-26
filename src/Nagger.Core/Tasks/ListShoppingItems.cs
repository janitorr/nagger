using Mediator;
using Nagger.Core.Tasks.Domain;

namespace Nagger.Core.Tasks;

public sealed record ListShoppingItemsQuery : IQuery<IReadOnlyList<ShoppingItem>>;

public sealed class ListShoppingItemsHandler(IShoppingItemStore store)
    : IQueryHandler<ListShoppingItemsQuery, IReadOnlyList<ShoppingItem>>
{
    public ValueTask<IReadOnlyList<ShoppingItem>> Handle(
        ListShoppingItemsQuery query,
        CancellationToken cancellationToken
    ) => store.GetAllAsync(cancellationToken);
}
