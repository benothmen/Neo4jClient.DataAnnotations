using System;
using System.Threading.Tasks;

namespace Neo4jClient.DataAnnotations.Transactions;

/// <summary>An explicit ORM transaction that owns its Neo4j session.</summary>
public interface IAnnotationsTransaction : IAnnotationsQueryRunner, IAsyncDisposable
{
    Task CommitAsync();
    Task RollbackAsync();
}
