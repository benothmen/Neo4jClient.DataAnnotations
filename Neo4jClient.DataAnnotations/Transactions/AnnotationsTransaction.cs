using System;
using System.Threading.Tasks;
using Neo4j.Driver;

namespace Neo4jClient.DataAnnotations.Transactions;

/// <summary>Explicit transaction implementation with deterministic session cleanup.</summary>
public sealed class AnnotationsTransaction : AnnotationsQueryRunner, IAnnotationsTransaction
{
    private readonly IAsyncSession session;
    private readonly IAsyncTransaction transaction;
    private bool disposed;

    internal AnnotationsTransaction(IAsyncSession session, IAsyncTransaction transaction)
        : base(transaction)
    {
        this.session = session ?? throw new ArgumentNullException(nameof(session));
        this.transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
    }

    public Task CommitAsync()
    {
        ThrowIfDisposed();
        return transaction.CommitAsync();
    }

    public Task RollbackAsync()
    {
        ThrowIfDisposed();
        return transaction.RollbackAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (disposed)
            return;

        disposed = true;
        try
        {
            await transaction.DisposeAsync();
        }
        finally
        {
            await session.DisposeAsync();
        }
    }

    private void ThrowIfDisposed()
    {
        if (disposed)
            throw new ObjectDisposedException(nameof(AnnotationsTransaction));
    }
}
