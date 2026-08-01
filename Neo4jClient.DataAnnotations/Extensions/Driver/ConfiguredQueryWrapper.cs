using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver;

namespace Neo4jClient.DataAnnotations.Extensions.Driver;

public class ConfiguredQueryWrapper<TIn, TOut> : IConfiguredQuery<TIn, TOut>
{
    protected IConfiguredQuery<TIn, TOut> ConfiguredQuery { get; }
    private readonly Func<TOut, TOut> wrapOutput;

    public ConfiguredQueryWrapper(
        IConfiguredQuery<TIn, TOut> query,
        Func<TOut, TOut> wrapOutput = null)
    {
        ConfiguredQuery = query ?? throw new ArgumentNullException(nameof(query));
        this.wrapOutput = wrapOutput ?? (value => value);
    }

    public IConfiguredQuery<TIn, TOut> WithFilter(Func<TOut, bool> filter)
    {
        if (filter == null)
            throw new ArgumentNullException(nameof(filter));

        return new ConfiguredQueryWrapper<TIn, TOut>(
            ConfiguredQuery.WithFilter(value => filter(wrapOutput(value))), wrapOutput);
    }

    public IConfiguredQuery<TOut, TNext> WithMap<TNext>(Func<TOut, TNext> map)
    {
        if (map == null)
            throw new ArgumentNullException(nameof(map));

        return new ConfiguredQueryWrapper<TOut, TNext>(
            ConfiguredQuery.WithMap(value => map(wrapOutput(value))));
    }

    public IReducedExecutableQuery<TResult> WithReduce<TResult>(
        Func<TResult> seed,
        Func<TResult, TOut, TResult> accumulate)
    {
        if (seed == null)
            throw new ArgumentNullException(nameof(seed));
        if (accumulate == null)
            throw new ArgumentNullException(nameof(accumulate));

        return new ReducedExecutableQueryWrapper<TResult>(
            ConfiguredQuery.WithReduce(seed,
                (result, value) => accumulate(result, wrapOutput(value))));
    }

    public IReducedExecutableQuery<TResult> WithReduce<TAccumulate, TResult>(
        Func<TAccumulate> seed,
        Func<TAccumulate, TOut, TAccumulate> accumulate,
        Func<TAccumulate, TResult> selectResult)
    {
        if (seed == null)
            throw new ArgumentNullException(nameof(seed));
        if (accumulate == null)
            throw new ArgumentNullException(nameof(accumulate));
        if (selectResult == null)
            throw new ArgumentNullException(nameof(selectResult));

        return new ReducedExecutableQueryWrapper<TResult>(
            ConfiguredQuery.WithReduce(seed,
                (result, value) => accumulate(result, wrapOutput(value)), selectResult));
    }

    public Task<EagerResult<IReadOnlyList<TOut>>> ExecuteAsync(
        CancellationToken token = default)
    {
        return ConfiguredQuery.WithMap(wrapOutput).ExecuteAsync(token);
    }
}
