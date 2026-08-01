using System;
using System.Threading.Tasks;
using Neo4j.Driver;
using Neo4jClient.Cypher;
using Neo4jClient.DataAnnotations.Cypher;
using Neo4jClient.DataAnnotations.Extensions.Driver;
using Neo4jClient.DataAnnotations.Transactions;
using Neo4jClient.DataAnnotations.Utils;
using NSubstitute;
using Xunit;

namespace Neo4jClient.DataAnnotations.Tests;

public class TransactionManagerTests
{
    [Theory]
    [MemberData(nameof(TestUtilities.TestContextData), MemberType = typeof(TestUtilities))]
    public async Task ManagedWrite_ExecutesFluentQueryAndRemovesInternalParameters(
        string testContextName, TestContext testContext)
    {
        var driver = Substitute.For<IDriver>();
        var session = Substitute.For<IAsyncSession>();
        var runner = Substitute.For<IAsyncQueryRunner>();
        var cursor = Substitute.For<IResultCursor>();
        Query capturedQuery = null;

        driver.AsyncSession().Returns(session);
        session.ExecuteWriteAsync(
                Arg.Any<Func<IAsyncQueryRunner, Task<int>>>(),
                Arg.Any<Action<TransactionConfigBuilder>>())
            .Returns(call => call.ArgAt<Func<IAsyncQueryRunner, Task<int>>>(0)(runner));
        runner.RunAsync(Arg.Any<Query>()).Returns(call =>
        {
            capturedQuery = call.Arg<Query>();
            return Task.FromResult(cursor);
        });

        var fluentQuery = testContext.Query
            .WithParam("name", "Alice")
            .WithParam(Defaults.QueryBuildStrategyKey, PropertiesBuildStrategy.WithParams);
        var manager = new AnnotationsTransactionManager(driver);

        var result = await manager.ExecuteWriteAsync(async transactionRunner =>
        {
            var wrappedCursor = await transactionRunner.RunAsync(fluentQuery);
            Assert.IsType<ResultCursorWrapper>(wrappedCursor);
            return 42;
        });

        Assert.Equal(42, result);
        Assert.Equal("Alice", capturedQuery.Parameters["name"]);
        Assert.False(capturedQuery.Parameters.ContainsKey(Defaults.QueryBuildStrategyKey));
        await session.Received(1).DisposeAsync();
    }

    [Fact]
    public async Task ExplicitTransaction_CommitsAndDisposesTransactionAndSession()
    {
        var driver = Substitute.For<IDriver>();
        var session = Substitute.For<IAsyncSession>();
        var transaction = Substitute.For<IAsyncTransaction>();

        driver.AsyncSession().Returns(session);
        session.BeginTransactionAsync().Returns(Task.FromResult(transaction));
        var manager = new AnnotationsTransactionManager(driver);

        await using (var ormTransaction = await manager.BeginTransactionAsync())
            await ormTransaction.CommitAsync();

        await transaction.Received(1).CommitAsync();
        await transaction.Received(1).DisposeAsync();
        await session.Received(1).DisposeAsync();
    }
}
