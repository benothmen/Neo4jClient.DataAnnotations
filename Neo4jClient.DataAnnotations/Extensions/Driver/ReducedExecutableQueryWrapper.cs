using System;
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver;

namespace Neo4jClient.DataAnnotations.Extensions.Driver;

public sealed class ReducedExecutableQueryWrapper<TResult> : IReducedExecutableQuery<TResult>
{
    private readonly IReducedExecutableQuery<TResult> query;

    public ReducedExecutableQueryWrapper(IReducedExecutableQuery<TResult> query)
    {
        this.query = query ?? throw new ArgumentNullException(nameof(query));
    }

    public Task<EagerResult<TResult>> ExecuteAsync(CancellationToken token = default)
    {
        return query.ExecuteAsync(token);
    }
}
