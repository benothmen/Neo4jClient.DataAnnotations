using Neo4jClient.Cypher;
using Neo4jClient.DataAnnotations.Serialization;
using Neo4jClient.DataAnnotations.Transactions;
using Neo4j.Driver;

namespace Neo4jClient.DataAnnotations
{
    internal interface IAnnotationsContext
    {
        IGraphClient GraphClient { get; }
        EntityService EntityService { get; }
        EntityResolver EntityResolver { get; }
        EntityResolverConverter EntityResolverConverter { get; }
        EntityConverter EntityConverter { get; }
        ICypherFluentQuery Cypher { get; }
        bool IsBoltClient { get; }
        IDriver Driver { get; }
        AnnotationsTransactionManager Transactions { get; }
    }
}
