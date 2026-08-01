using System;
using Neo4jClient.DataAnnotations.Tests.Models;
using Xunit;

namespace Neo4jClient.DataAnnotations.Tests;

public class CypherFunctionCoverageTests
{
    [Theory]
    [MemberData(nameof(TestUtilities.TestContextData), MemberType = typeof(TestUtilities))]
    public void ModernScalarAndSpatialFunctions_AreRendered(
        string testContextName, TestContext testContext)
    {
        var query = testContext.Query
            .With((ActorNode actor) => new
            {
                elementId = CypherFunctions.ElementId(actor),
                valueType = CypherFunctions.ValueType(actor.Name),
                convertedYear = CypherFunctions.ToIntegerOrNull(actor.Name),
                emptyRoles = CypherFunctions.IsEmpty(actor.Roles),
                distance = CypherFunctions.Distance(
                    CypherFunctions.Point(new { latitude = 1.0, longitude = 2.0 }),
                    CypherFunctions.Point(new { latitude = 3.0, longitude = 4.0 }))
            })
            .Query.QueryText;

        Assert.Contains("elementId(actor) AS elementId", query);
        Assert.Contains("valueType(", query);
        Assert.Contains(") AS valueType", query);
        Assert.Contains("toIntegerOrNull(", query);
        Assert.Contains(") AS convertedYear", query);
        Assert.Contains("isEmpty(", query);
        Assert.Contains(") AS emptyRoles", query);
        Assert.Contains("point.distance(point(", query);
    }

    [Theory]
    [MemberData(nameof(TestUtilities.TestContextData), MemberType = typeof(TestUtilities))]
    public void GenericFunctionCall_RendersQualifiedFunctionNameAndArguments(
        string testContextName, TestContext testContext)
    {
        var query = testContext.Query
            .With((ActorNode actor) => new
            {
                score = CypherFunctions.Call<double>(
                    "vector.similarity.cosine", actor.Roles, actor.Roles)
            })
            .Query.QueryText;

        Assert.Contains(
            "vector.similarity.cosine(actor.Roles, actor.Roles) AS score", query);
    }

    [Theory]
    [MemberData(nameof(TestUtilities.TestContextData), MemberType = typeof(TestUtilities))]
    public void GenericFunctionCall_RejectsCypherFragments(
        string testContextName, TestContext testContext)
    {
        Assert.Throws<InvalidOperationException>(() => testContext.Query
            .With((ActorNode actor) => new
            {
                value = CypherFunctions.Call<object>("evil) RETURN actor //", actor)
            })
            .Query.QueryText);
    }
}
