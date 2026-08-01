using System.Collections.Generic;
using System.Threading.Tasks;
using Neo4j.Driver;
using Neo4jClient.Cypher;

namespace Neo4jClient.DataAnnotations.Transactions;

/// <summary>Runs raw or annotation-aware fluent Cypher within an ORM transaction.</summary>
public interface IAnnotationsQueryRunner
{
    Task<IResultCursor> RunAsync(string query);
    Task<IResultCursor> RunAsync(string query, object parameters);
    Task<IResultCursor> RunAsync(string query, IDictionary<string, object> parameters);
    Task<IResultCursor> RunAsync(Query query);
    Task<IResultCursor> RunAsync(ICypherFluentQuery query);
}
