using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Neo4j.Driver;
using Neo4jClient.Cypher;
using Neo4jClient.DataAnnotations.Extensions.Driver;
using Neo4jClient.DataAnnotations.Utils;

namespace Neo4jClient.DataAnnotations.Transactions;

/// <summary>Adapts a Neo4j query runner to the annotation-aware ORM query surface.</summary>
public class AnnotationsQueryRunner : IAnnotationsQueryRunner
{
    protected IAsyncQueryRunner QueryRunner { get; }

    public AnnotationsQueryRunner(IAsyncQueryRunner queryRunner)
    {
        QueryRunner = SessionWrapper.GetAsyncQueryRunner(
            queryRunner ?? throw new ArgumentNullException(nameof(queryRunner)));
    }

    public Task<IResultCursor> RunAsync(string query)
    {
        return QueryRunner.RunAsync(query);
    }

    public Task<IResultCursor> RunAsync(string query, object parameters)
    {
        return QueryRunner.RunAsync(query, parameters);
    }

    public Task<IResultCursor> RunAsync(string query, IDictionary<string, object> parameters)
    {
        return QueryRunner.RunAsync(query, parameters);
    }

    public Task<IResultCursor> RunAsync(Query query)
    {
        return QueryRunner.RunAsync(query);
    }

    public Task<IResultCursor> RunAsync(ICypherFluentQuery query)
    {
        if (query == null)
            throw new ArgumentNullException(nameof(query));

        var cypherQuery = query.Query;
        var parameters = cypherQuery.QueryParameters
            .Where(parameter => parameter.Key != Defaults.QueryBuildStrategyKey)
            .ToDictionary(parameter => parameter.Key, parameter => parameter.Value);

        return RunAsync(new Query(cypherQuery.QueryText, parameters));
    }
}
