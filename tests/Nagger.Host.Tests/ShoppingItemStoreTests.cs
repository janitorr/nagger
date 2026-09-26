using Microsoft.EntityFrameworkCore;
using Nagger.Core.Tasks.Domain;
using Nagger.Host.Infrastructure;
using Shouldly;

namespace Nagger.Host.Tests;

public sealed class ShoppingItemStoreTests
{
    [Fact]
    public async Task SqliteShoppingItemStore_GivenItems_WhenAddedQueriedAndRemoved_ThenPersistsInAscendingIdOrder()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"nagger-{Guid.NewGuid():N}.db");
        try
        {
            await using var database = CreateContext(databasePath);
            await database.Database.MigrateAsync();
            var store = new SqliteShoppingItemStore(database);

            var milk = await store.AddAsync(new ShoppingItem(0, "milk"), default);
            var yogurt = await store.AddAsync(new ShoppingItem(0, "yogurt"), default);

            milk.Id.ShouldBe(1);
            yogurt.Id.ShouldBe(2);
            (await store.GetAllAsync(default)).Select(x => x.Name).ShouldBe(["milk", "yogurt"]);

            (await store.GetByNameAsync("MILK", default)).ShouldBe(milk);

            await store.RemoveAsync(milk, default);
            (await store.GetAllAsync(default)).Select(x => x.Name).ShouldBe(["yogurt"]);
        }
        finally
        {
            if (File.Exists(databasePath))
                File.Delete(databasePath);
        }
    }

    [Fact]
    public async Task SqliteShoppingItemStore_GivenCaseVariantName_WhenAdded_ThenRejectsDuplicate()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"nagger-{Guid.NewGuid():N}.db");
        try
        {
            await using var database = CreateContext(databasePath);
            await database.Database.MigrateAsync();
            var store = new SqliteShoppingItemStore(database);
            await store.AddAsync(new ShoppingItem(0, "milk"), default);

            await Should.ThrowAsync<DbUpdateException>(async () =>
                await store.AddAsync(new ShoppingItem(0, "Milk"), default)
            );
        }
        finally
        {
            if (File.Exists(databasePath))
                File.Delete(databasePath);
        }
    }

    private static NaggerDbContext CreateContext(string databasePath)
    {
        var options = new DbContextOptionsBuilder<NaggerDbContext>().UseSqlite($"Data Source={databasePath}").Options;
        return new NaggerDbContext(options);
    }
}
