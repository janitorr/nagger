using Microsoft.EntityFrameworkCore;
using Nagger.Core.Tasks;
using Nagger.Core.Tasks.Domain;

namespace Nagger.Host.Infrastructure;

public sealed class SqliteShoppingItemStore(NaggerDbContext database) : IShoppingItemStore
{
    public async ValueTask<ShoppingItem> AddAsync(ShoppingItem item, CancellationToken cancellationToken)
    {
        var entity = new ShoppingItemEntity { Name = item.Name };
        database.ShoppingItems.Add(entity);
        await database.SaveChangesAsync(cancellationToken);
        return ToModel(entity);
    }

    public async ValueTask<ShoppingItem?> GetByNameAsync(string name, CancellationToken cancellationToken)
    {
        var entity = await database
            .ShoppingItems.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Name == name, cancellationToken);
        return entity is null ? null : ToModel(entity);
    }

    public async ValueTask RemoveAsync(ShoppingItem item, CancellationToken cancellationToken)
    {
        var entity = await database.ShoppingItems.SingleAsync(x => x.Id == item.Id, cancellationToken);
        database.ShoppingItems.Remove(entity);
        await database.SaveChangesAsync(cancellationToken);
    }

    public async ValueTask<IReadOnlyList<ShoppingItem>> GetAllAsync(CancellationToken cancellationToken) =>
        (await database.ShoppingItems.AsNoTracking().OrderBy(x => x.Id).ToListAsync(cancellationToken))
            .Select(ToModel)
            .ToList();

    private static ShoppingItem ToModel(ShoppingItemEntity entity) => new(entity.Id, entity.Name);
}
