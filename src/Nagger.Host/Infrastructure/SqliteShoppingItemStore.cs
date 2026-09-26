using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Nagger.Core.Tasks;
using Nagger.Core.Tasks.Domain;

namespace Nagger.Host.Infrastructure;

public sealed class SqliteShoppingItemStore(NaggerDbContext database) : IShoppingItemStore
{
    public async ValueTask<(ShoppingItem Item, bool Created)> AddIfAbsentAsync(
        ShoppingItem item,
        CancellationToken cancellationToken
    )
    {
        var entity = new ShoppingItemEntity { Name = item.Name };
        database.ShoppingItems.Add(entity);
        try
        {
            await database.SaveChangesAsync(cancellationToken);
            return (ToModel(entity), true);
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            database.ChangeTracker.Clear();
            var existing = await FindByNameAsync(item.Name, cancellationToken);
            return (existing!, false);
        }
    }

    public async ValueTask RemoveByNameAsync(string name, CancellationToken cancellationToken)
    {
        await database.ShoppingItems.Where(x => x.Name == name).ExecuteDeleteAsync(cancellationToken);
    }

    public async ValueTask<IReadOnlyList<ShoppingItem>> GetAllAsync(CancellationToken cancellationToken) =>
        (await database.ShoppingItems.AsNoTracking().OrderBy(x => x.Id).ToListAsync(cancellationToken))
            .Select(ToModel)
            .ToList();

    private async ValueTask<ShoppingItem?> FindByNameAsync(string name, CancellationToken cancellationToken)
    {
        var entity = await database
            .ShoppingItems.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Name == name, cancellationToken);
        return entity is null ? null : ToModel(entity);
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception) =>
        exception.InnerException is SqliteException { SqliteExtendedErrorCode: 2067 };

    private static ShoppingItem ToModel(ShoppingItemEntity entity) => new(entity.Id, entity.Name);
}
