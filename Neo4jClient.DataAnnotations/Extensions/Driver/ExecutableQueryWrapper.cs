using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver;

namespace Neo4jClient.DataAnnotations.Extensions.Driver;

/// <summary>Preserves annotation-aware records throughout the executable-query pipeline.</summary>
public sealed class ExecutableQueryWrapper
    : ConfiguredQueryWrapper<IRecord, IRecord>, IExecutableQuery<IRecord, IRecord>
{
    private readonly IExecutableQuery<IRecord, IRecord> executableQuery;

    public ExecutableQueryWrapper(IExecutableQuery<IRecord, IRecord> query)
        : base(query, ResultCursorWrapper.GetRecord)
    {
        executableQuery = query ?? throw new ArgumentNullException(nameof(query));
    }

    public IExecutableQuery<IRecord, IRecord> WithConfig(QueryConfig config)
    {
        return new ExecutableQueryWrapper(executableQuery.WithConfig(config));
    }

    public IExecutableQuery<IRecord, IRecord> WithParameters(
        Dictionary<string, object> parameters)
    {
        return new ExecutableQueryWrapper(executableQuery.WithParameters(
            AsyncQueryRunnerWrapper.NormalizeParameters(parameters)));
    }

    public IExecutableQuery<IRecord, IRecord> WithParameters(object parameters)
    {
        return new ExecutableQueryWrapper(executableQuery.WithParameters(
            AsyncQueryRunnerWrapper.NormalizeValue(parameters)));
    }

    public IReducedExecutableQuery<TResult> WithStreamProcessor<TResult>(
        Func<IAsyncEnumerable<IRecord>, Task<TResult>> streamProcessor)
    {
        if (streamProcessor == null)
            throw new ArgumentNullException(nameof(streamProcessor));

        return new ReducedExecutableQueryWrapper<TResult>(
            executableQuery.WithStreamProcessor(
                records => streamProcessor(WrapRecords(records))));
    }

    private static async IAsyncEnumerable<IRecord> WrapRecords(
        IAsyncEnumerable<IRecord> records,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var record in records.WithCancellation(cancellationToken))
            yield return ResultCursorWrapper.GetRecord(record);
    }
}
