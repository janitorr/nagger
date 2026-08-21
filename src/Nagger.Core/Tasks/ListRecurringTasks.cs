using Mediator;
using Nagger.Core.Tasks.Domain;

namespace Nagger.Core.Tasks;

public sealed record ListRecurringTemplatesQuery : IQuery<IReadOnlyList<RecurringTaskTemplate>>;

public sealed class ListRecurringTemplatesHandler(IRecurringTaskTemplateStore store)
    : IQueryHandler<ListRecurringTemplatesQuery, IReadOnlyList<RecurringTaskTemplate>>
{
    public ValueTask<IReadOnlyList<RecurringTaskTemplate>> Handle(
        ListRecurringTemplatesQuery query,
        CancellationToken cancellationToken
    ) => store.GetAllAsync(cancellationToken);
}
