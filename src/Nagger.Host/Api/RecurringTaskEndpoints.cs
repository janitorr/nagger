using System.Globalization;
using Mediator;
using Nagger.Core.Tasks;
using Nagger.Core.Tasks.Domain;

namespace Nagger.Host.Api;

public static class RecurringTaskEndpoints
{
    public static void MapRecurringTaskEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/tasks/recurring");

        group.MapPost(
            "",
            async (CreateRecurringTaskCommand command, IMediator mediator, CancellationToken cancellationToken) =>
            {
                var result = await mediator.Send(command, cancellationToken);
                return Results.Created(
                    $"/tasks/recurring/{result.Template.Id}",
                    RecurringCreationResponse.From(result)
                );
            }
        );

        group.MapGet(
            "",
            async (IMediator mediator, CancellationToken cancellationToken) =>
                Results.Ok(
                    (await mediator.Send(new ListRecurringTemplatesQuery(), cancellationToken))
                        .Select(RecurringTemplateResponse.From)
                        .ToList()
                )
        );

        group.MapPost(
            "{id:long}/complete",
            async (long id, IMediator mediator, CancellationToken cancellationToken) =>
                Results.Ok(
                    RecurringCompletionResponse.From(
                        await mediator.Send(new CompleteRecurringTaskCommand(id), cancellationToken)
                    )
                )
        );

        group.MapPost(
            "{id:long}/pause",
            async (long id, IMediator mediator, CancellationToken cancellationToken) =>
                Results.Ok(
                    RecurringTemplateResponse.From(
                        await mediator.Send(new PauseRecurringTaskCommand(id), cancellationToken)
                    )
                )
        );

        group.MapPost(
            "{id:long}/resume",
            async (long id, IMediator mediator, CancellationToken cancellationToken) =>
                Results.Ok(
                    RecurringTemplateResponse.From(
                        await mediator.Send(new ResumeRecurringTaskCommand(id), cancellationToken)
                    )
                )
        );

        group.MapPost(
            "{id:long}/cancel",
            async (long id, IMediator mediator, CancellationToken cancellationToken) =>
                Results.Ok(
                    RecurringTemplateResponse.From(
                        await mediator.Send(new CancelRecurringTaskCommand(id), cancellationToken)
                    )
                )
        );
    }
}

public sealed record RecurringTemplateResponse(
    long Id,
    string Title,
    string StartDate,
    RecurrenceRuleResponse Recurrence,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? CancelledAt
)
{
    public static RecurringTemplateResponse From(RecurringTaskTemplate template) =>
        new(
            template.Id,
            template.Title,
            template.StartDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            new RecurrenceRuleResponse(template.Recurrence.Every, template.Recurrence.Unit.ToContractValue()),
            template.Status.ToContractValue(),
            template.CreatedAt,
            template.UpdatedAt,
            template.CancelledAt
        );
}

public sealed record RecurrenceRuleResponse(int Every, string Unit);

public sealed record RecurringCreationResponse(
    RecurringTemplateResponse Template,
    RecurringTaskInstanceResponse FirstInstance
)
{
    public static RecurringCreationResponse From(CreateRecurringTaskResult result) =>
        new(RecurringTemplateResponse.From(result.Template), RecurringTaskInstanceResponse.From(result.FirstInstance));
}

public sealed record RecurringCompletionResponse(
    RecurringTaskInstanceResponse CompletedInstance,
    RecurringTaskInstanceResponse NextInstance
)
{
    public static RecurringCompletionResponse From(CompleteRecurringTaskResult result) =>
        new(
            RecurringTaskInstanceResponse.From(result.CompletedInstance),
            RecurringTaskInstanceResponse.From(result.NextInstance)
        );
}

public sealed record RecurringTaskInstanceResponse(
    long Id,
    long RecurringTaskId,
    string Title,
    string Type,
    string Status,
    DateTimeOffset DueAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? CompletedAt,
    DateTimeOffset? CancelledAt
)
{
    public static RecurringTaskInstanceResponse From(RecurringTaskInstance instance) =>
        new(
            instance.Id,
            instance.RecurringTaskId,
            instance.Title,
            "recurring",
            instance.Status.ToContractValue(),
            instance.DueAt,
            instance.CreatedAt,
            instance.UpdatedAt,
            instance.CompletedAt,
            instance.CancelledAt
        );
}
