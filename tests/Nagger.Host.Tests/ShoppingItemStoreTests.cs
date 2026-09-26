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

            var (milk, milkCreated) = await store.AddIfAbsentAsync(new ShoppingItem(0, "milk"), default);
            var (yogurt, _) = await store.AddIfAbsentAsync(new ShoppingItem(0, "yogurt"), default);

            milkCreated.ShouldBeTrue();
            milk.Id.ShouldBe(1);
            yogurt.Id.ShouldBe(2);
            (await store.GetAllAsync(default)).Select(x => x.Name).ShouldBe(["milk", "yogurt"]);

            await store.RemoveByNameAsync("MILK", default);

            (await store.GetAllAsync(default)).Select(x => x.Name).ShouldBe(["yogurt"]);
        }
        finally
        {
            if (File.Exists(databasePath))
                File.Delete(databasePath);
        }
    }

    [Fact]
    public async Task SqliteShoppingItemStore_GivenCaseVariantName_WhenAdded_ThenReturnsExistingWithoutDuplicate()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"nagger-{Guid.NewGuid():N}.db");
        try
        {
            await using var database = CreateContext(databasePath);
            await database.Database.MigrateAsync();
            var store = new SqliteShoppingItemStore(database);

            var (first, firstCreated) = await store.AddIfAbsentAsync(new ShoppingItem(0, "milk"), default);
            var (second, secondCreated) = await store.AddIfAbsentAsync(new ShoppingItem(0, "Milk"), default);

            firstCreated.ShouldBeTrue();
            secondCreated.ShouldBeFalse();
            second.ShouldBe(first);
            (await store.GetAllAsync(default)).ShouldHaveSingleItem();
        }
        finally
        {
            if (File.Exists(databasePath))
                File.Delete(databasePath);
        }
    }

    [Fact]
    public async Task SqliteShoppingItemStore_GivenAbsentName_WhenRemoved_ThenLeavesListUnchanged()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"nagger-{Guid.NewGuid():N}.db");
        try
        {
            await using var database = CreateContext(databasePath);
            await database.Database.MigrateAsync();
            var store = new SqliteShoppingItemStore(database);
            await store.AddIfAbsentAsync(new ShoppingItem(0, "milk"), default);

            await store.RemoveByNameAsync("yogurt", default);

            (await store.GetAllAsync(default)).Select(x => x.Name).ShouldBe(["milk"]);
        }
        finally
        {
            if (File.Exists(databasePath))
                File.Delete(databasePath);
        }
    }

    // SQLite's NOCASE collation folds ASCII case only, so non-ASCII case stays distinct (documented, accepted limitation).
    [Fact]
    public async Task SqliteShoppingItemStore_GivenNonAsciiCaseVariantName_WhenAdded_ThenKeepsDistinctItems()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"nagger-{Guid.NewGuid():N}.db");
        try
        {
            await using var database = CreateContext(databasePath);
            await database.Database.MigrateAsync();
            var store = new SqliteShoppingItemStore(database);

            var (_, upperCreated) = await store.AddIfAbsentAsync(new ShoppingItem(0, "NÜSSE"), default);
            var (_, lowerCreated) = await store.AddIfAbsentAsync(new ShoppingItem(0, "nüsse"), default);

            upperCreated.ShouldBeTrue();
            lowerCreated.ShouldBeTrue();
            (await store.GetAllAsync(default)).Select(x => x.Name).ShouldBe(["NÜSSE", "nüsse"]);
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
