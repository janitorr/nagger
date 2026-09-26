using Mediator;
using Nagger.Core.Tasks;
using Nagger.Core.Tasks.Domain;

namespace Nagger.Host.Api;

public static class ShoppingEndpoints
{
    public static void MapShoppingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/shopping");

        group.MapPost(
            "",
            async (AddShoppingItemRequest request, IMediator mediator, CancellationToken cancellationToken) =>
            {
                var result = await mediator.Send(new AddShoppingItemCommand(request.Name), cancellationToken);
                var response = ShoppingItemResponse.From(result.Item);
                return result.Created ? Results.Created($"/shopping/{result.Item.Id}", response) : Results.Ok(response);
            }
        );

        group.MapGet(
            "",
            async (IMediator mediator, CancellationToken cancellationToken) =>
                Results.Ok(
                    (await mediator.Send(new ListShoppingItemsQuery(), cancellationToken))
                        .Select(ShoppingItemResponse.From)
                        .ToList()
                )
        );

        group.MapDelete(
            "{name}",
            async (string name, IMediator mediator, CancellationToken cancellationToken) =>
            {
                await mediator.Send(new RemoveShoppingItemCommand(name), cancellationToken);
                return Results.NoContent();
            }
        );
    }
}

public sealed record AddShoppingItemRequest(string? Name);

public sealed record ShoppingItemResponse(long Id, string Name)
{
    public static ShoppingItemResponse From(ShoppingItem item) => new(item.Id, item.Name);
}
