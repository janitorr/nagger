using Mediator;
using Nagger.Host.Infrastructure;

namespace Nagger.Host.Composition.Mediator;

public sealed class TransactionBehavior<TMessage, TResponse>(NaggerDbContext dbContext)
    : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IBaseCommand
{
    public async ValueTask<TResponse> Handle(
        TMessage message,
        MessageHandlerDelegate<TMessage, TResponse> next,
        CancellationToken cancellationToken
    )
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var response = await next(message, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return response;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
