using Nagger.Core.Tasks;
using Nagger.Core.Tasks.Domain;
using Shouldly;

namespace Nagger.Core.Tests;

public sealed class ShoppingListFeatureTests
{
    [Fact]
    public async Task AddShoppingItem_GivenNewName_WhenAddRequested_ThenPersistsItemWithAssignedId()
    {
        var store = new MemoryShoppingItemStore();

        var result = await new AddShoppingItemHandler(store).Handle(new("milk"), default);

        result.Created.ShouldBeTrue();
        result.Item.Id.ShouldBe(1);
        result.Item.Name.ShouldBe("milk");
        store.Items.ShouldHaveSingleItem().ShouldBe(result.Item);
    }

    [Fact]
    public async Task AddShoppingItem_GivenNewNameWithWhitespace_WhenAddRequested_ThenPersistsTrimmedNamePreservingCase()
    {
        var store = new MemoryShoppingItemStore();

        var result = await new AddShoppingItemHandler(store).Handle(new("  Whole Milk  "), default);

        result.Item.Name.ShouldBe("Whole Milk");
        result.Created.ShouldBeTrue();
        store.Items.ShouldHaveSingleItem().Name.ShouldBe("Whole Milk");
    }

    [Theory]
    [InlineData("milk")]
    [InlineData("Milk")]
    [InlineData("  MILK  ")]
    public async Task AddShoppingItem_GivenExistingNameVariant_WhenAddRequested_ThenReturnsExistingWithoutDuplicate(
        string variant
    )
    {
        var store = new MemoryShoppingItemStore(new ShoppingItem(1, "milk"));

        var result = await new AddShoppingItemHandler(store).Handle(new(variant), default);

        result.Created.ShouldBeFalse();
        result.Item.ShouldBe(new ShoppingItem(1, "milk"));
        store.Items.ShouldHaveSingleItem();
        store.Adds.ShouldBe(0);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(" ")]
    public async Task AddShoppingItem_GivenBlankName_WhenAddRequested_ThenRejectsWithoutPersisting(string? name)
    {
        var store = new MemoryShoppingItemStore();

        var exception = await Should.ThrowAsync<ValidationException>(async () =>
            await new AddShoppingItemHandler(store).Handle(new(name), default)
        );

        exception.Errors["name"].ShouldBe(["Name is required."]);
        store.Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task AddShoppingItem_GivenTooLongName_WhenAddRequested_ThenRejectsWithoutPersisting()
    {
        var store = new MemoryShoppingItemStore();

        var exception = await Should.ThrowAsync<ValidationException>(async () =>
            await new AddShoppingItemHandler(store).Handle(new(new string('a', 201)), default)
        );

        exception.Errors["name"].ShouldBe(["Name must be at most 200 characters."]);
        store.Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task AddShoppingItem_GivenMaxLengthName_WhenAddRequested_ThenPersistsItem()
    {
        var store = new MemoryShoppingItemStore();
        var name = new string('a', 200);

        var result = await new AddShoppingItemHandler(store).Handle(new(name), default);

        result.Item.Name.ShouldBe(name);
    }

    [Fact]
    public async Task RemoveShoppingItem_GivenExistingName_WhenRemoveRequested_ThenRemovesItem()
    {
        var store = new MemoryShoppingItemStore(new ShoppingItem(1, "milk"));

        var name = await new RemoveShoppingItemHandler(store).Handle(new("Milk"), default);

        name.ShouldBe("Milk");
        store.Items.ShouldBeEmpty();
        store.Removes.ShouldBe(1);
    }

    [Fact]
    public async Task RemoveShoppingItem_GivenAbsentName_WhenRemoveRequested_ThenLeavesListUnchanged()
    {
        var store = new MemoryShoppingItemStore(new ShoppingItem(1, "milk"));

        await new RemoveShoppingItemHandler(store).Handle(new("yogurt"), default);

        store.Items.ShouldHaveSingleItem().Name.ShouldBe("milk");
        store.Removes.ShouldBe(0);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(" ")]
    public async Task RemoveShoppingItem_GivenBlankName_WhenRemoveRequested_ThenRejectsWithoutRemoving(string? name)
    {
        var store = new MemoryShoppingItemStore(new ShoppingItem(1, "milk"));

        var exception = await Should.ThrowAsync<ValidationException>(async () =>
            await new RemoveShoppingItemHandler(store).Handle(new(name), default)
        );

        exception.Errors["name"].ShouldBe(["Name is required."]);
        store.Items.ShouldHaveSingleItem();
        store.Removes.ShouldBe(0);
    }

    [Fact]
    public async Task ListShoppingItems_GivenItems_WhenRequested_ThenReturnsInAscendingIdOrder()
    {
        var store = new MemoryShoppingItemStore(new ShoppingItem(2, "yogurt"), new ShoppingItem(1, "milk"));

        var items = await new ListShoppingItemsHandler(store).Handle(new(), default);

        items.Select(x => x.Id).ShouldBe([1, 2]);
    }

    [Fact]
    public async Task ListShoppingItems_GivenNoItems_WhenRequested_ThenReturnsEmptyList()
    {
        var items = await new ListShoppingItemsHandler(new MemoryShoppingItemStore()).Handle(new(), default);

        items.ShouldBeEmpty();
    }
}
