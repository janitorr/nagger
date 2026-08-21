using Mediator;
using Nagger.Core.Tasks.Domain;

namespace Nagger.Core.Tasks;

public sealed record ListOpenOneShotTasksQuery : IQuery<IReadOnlyList<TaskItem>>;

public sealed class ListOpenOneShotTasksHandler(ITaskStore store)
    : IQueryHandler<ListOpenOneShotTasksQuery, IReadOnlyList<TaskItem>>
{
    public ValueTask<IReadOnlyList<TaskItem>> Handle(ListOpenOneShotTasksQuery query, CancellationToken cancellationToken) =>
        store.GetOpenOneShotTasksAsync(cancellationToken);
}
