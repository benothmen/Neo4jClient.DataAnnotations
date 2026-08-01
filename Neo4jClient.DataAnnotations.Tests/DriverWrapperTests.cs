using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver;
using Neo4jClient.DataAnnotations.Extensions.Driver;
using Neo4jClient.DataAnnotations.Utils;
using Newtonsoft.Json.Linq;
using NSubstitute;
using Xunit;

namespace Neo4jClient.DataAnnotations.Tests;

public class DriverWrapperTests
{
    [Fact]
    public void EntityWrappers_DelegateTypedPropertyAccess()
    {
        var node = new FakeNode(new Dictionary<string, object>
        {
            ["name"] = "Alice",
            ["age"] = 42
        });
        var relationship = new FakeRelationship(new Dictionary<string, object>
        {
            ["role"] = "Lead"
        });

        var nodeWrapper = new NodeWrapper(node);
        var relationshipWrapper = new RelationshipWrapper(relationship);

        Assert.Equal("Alice", nodeWrapper.Get<string>("name"));
        Assert.True(nodeWrapper.TryGet("age", out int age));
        Assert.Equal(42, age);
        Assert.Equal("Lead", relationshipWrapper.Get<string>("role"));
        Assert.True(relationshipWrapper.TryGet("role", out string role));
        Assert.Equal("Lead", role);
        Assert.Contains(Defaults.BoltMetadataPropertyName, nodeWrapper.Properties.Keys);
        Assert.Contains(Defaults.BoltMetadataPropertyName, relationshipWrapper.Properties.Keys);
    }

    [Fact]
    public void RecordWrapper_ImplementsDictionaryAndWrapsNestedEntities()
    {
        var node = new FakeNode(new Dictionary<string, object> { ["name"] = "Alice" });
        var relationship = new FakeRelationship(new Dictionary<string, object> { ["role"] = "Lead" });
        var path = new FakePath(node, node, relationship);
        var rawValues = new Dictionary<string, object>
        {
            ["node"] = node,
            ["relationship"] = relationship,
            ["path"] = path,
            ["nodes"] = new List<INode> { node },
            ["nested"] = new Dictionary<string, object> { ["node"] = node }
        };
        var wrapper = new RecordWrapper(new FakeRecord(rawValues));

        Assert.Equal(5, wrapper.Count);
        Assert.True(wrapper.ContainsKey("node"));
        Assert.IsType<NodeWrapper>(wrapper["node"]);
        Assert.IsType<NodeWrapper>(wrapper[0]);
        Assert.IsType<NodeWrapper>(wrapper.Get<INode>("node"));
        Assert.IsType<NodeWrapper>(wrapper.GetCaseInsensitive<INode>("NODE"));
        Assert.True(wrapper.TryGet("relationship", out IRelationship wrappedRelationship));
        Assert.IsType<RelationshipWrapper>(wrappedRelationship);
        Assert.True(wrapper.TryGetValue("nested", out var nested));
        Assert.IsType<NodeWrapper>(((IReadOnlyDictionary<string, object>)nested)["node"]);
        var wrappedPath = Assert.IsType<PathWrapper>(wrapper.Get<IPath>("path"));
        Assert.IsType<NodeWrapper>(wrappedPath.Start);
        Assert.IsType<NodeWrapper>(wrappedPath.End);
        Assert.All(wrappedPath.Nodes, value => Assert.IsType<NodeWrapper>(value));
        Assert.All(wrappedPath.Relationships,
            value => Assert.IsType<RelationshipWrapper>(value));
        Assert.All(wrapper.Get<IReadOnlyList<INode>>("nodes"), value => Assert.IsType<NodeWrapper>(value));
        Assert.Equal(5, wrapper.ToList().Count);

        Assert.Same(node, rawValues["node"]);
    }

    [Fact]
    public async Task ResultCursorWrapper_WrapsRecordsDuringAsyncEnumeration()
    {
        var record = new FakeRecord(new Dictionary<string, object> { ["value"] = 1 });
        var wrapper = new ResultCursorWrapper(new FakeResultCursor(record));

        var results = new List<IRecord>();
        await foreach (var result in wrapper)
            results.Add(result);

        Assert.Single(results);
        Assert.IsType<RecordWrapper>(results[0]);
    }

    [Fact]
    public async Task SessionWrapper_WrapsExplicitAndManagedTransactions()
    {
        var session = Substitute.For<IAsyncSession>();
        var transaction = Substitute.For<IAsyncTransaction>();
        var queryRunner = Substitute.For<IAsyncQueryRunner>();
        session.BeginTransactionAsync().Returns(Task.FromResult(transaction));
        session.ExecuteReadAsync(
                Arg.Any<Func<IAsyncQueryRunner, Task<IAsyncQueryRunner>>>(),
                Arg.Any<Action<TransactionConfigBuilder>>())
            .Returns(call => call.ArgAt<Func<IAsyncQueryRunner, Task<IAsyncQueryRunner>>>(0)(queryRunner));
        var wrapper = new SessionWrapper(session);

        var explicitTransaction = await wrapper.BeginTransactionAsync();
        var managedRunner = await wrapper.ExecuteReadAsync(runner => Task.FromResult(runner));

        Assert.IsType<AsyncTransactionWrapper>(explicitTransaction);
        Assert.IsType<AsyncQueryRunnerWrapper>(managedRunner);
    }

    [Fact]
    public async Task AsyncQueryRunnerWrapper_ConvertsJObjectParametersAndWrapsCursor()
    {
        var rawRunner = Substitute.For<IAsyncQueryRunner>();
        var rawCursor = new FakeResultCursor();
        Query capturedQuery = null;
        rawRunner.RunAsync(Arg.Any<Query>()).Returns(call =>
        {
            capturedQuery = call.Arg<Query>();
            return Task.FromResult<IResultCursor>(rawCursor);
        });
        var wrapper = new AsyncQueryRunnerWrapper(rawRunner);
        var query = new Query("RETURN $entity", new Dictionary<string, object>
        {
            ["entity"] = JObject.FromObject(new
            {
                Name = "Alice",
                Address = new { City = "London" },
                Tags = new[] { "admin", "author" }
            })
        });

        var result = await wrapper.RunAsync(query);

        Assert.IsType<ResultCursorWrapper>(result);
        var entity = Assert.IsType<Dictionary<string, object>>(capturedQuery.Parameters["entity"]);
        Assert.IsType<Dictionary<string, object>>(entity["Address"]);
        Assert.IsAssignableFrom<IReadOnlyList<object>>(entity["Tags"]);
    }

    [Fact]
    public async Task DriverWrapper_ExecutableQueryWrapsRecordsAndNormalizesNestedParameters()
    {
        var rawQuery = Substitute.For<IExecutableQuery<IRecord, IRecord>>();
        var mappedQuery = Substitute.For<IConfiguredQuery<IRecord, IRecord>>();
        Dictionary<string, object> capturedParameters = null;
        Func<IRecord, IRecord> capturedMapper = null;

        rawQuery.WithParameters(Arg.Do<Dictionary<string, object>>(
                value => capturedParameters = value))
            .Returns(rawQuery);
        rawQuery.WithMap<IRecord>(Arg.Do<Func<IRecord, IRecord>>(
                value => capturedMapper = value))
            .Returns(mappedQuery);
        mappedQuery.ExecuteAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<EagerResult<IReadOnlyList<IRecord>>>(null));

        var rawDriver = Substitute.For<IDriver>();
        rawDriver.ExecutableQuery("RETURN $entity").Returns(rawQuery);
        var query = new DriverWrapper(rawDriver)
            .ExecutableQuery("RETURN $entity")
            .WithParameters(new Dictionary<string, object>
            {
                ["entity"] = JObject.FromObject(new
                {
                    Name = "Alice",
                    Roles = new[] { "admin" }
                })
            });

        await query.ExecuteAsync();

        var entity = Assert.IsType<Dictionary<string, object>>(capturedParameters["entity"]);
        Assert.IsAssignableFrom<IReadOnlyList<object>>(entity["Roles"]);
        Assert.IsType<RecordWrapper>(capturedMapper(
            new FakeRecord(new Dictionary<string, object> { ["value"] = 1 })));
    }

    [Fact]
    public async Task SessionWrapper_ConvertsJObjectParametersAndWrapsCursor()
    {
        var rawSession = Substitute.For<IAsyncSession>();
        var rawCursor = new FakeResultCursor();
        Query capturedQuery = null;
        rawSession.RunAsync(Arg.Any<Query>()).Returns(call =>
        {
            capturedQuery = call.Arg<Query>();
            return Task.FromResult<IResultCursor>(rawCursor);
        });
        var wrapper = new SessionWrapper(rawSession);
        var query = new Query("RETURN $entity", new Dictionary<string, object>
        {
            ["entity"] = JObject.FromObject(new { Name = "Alice" })
        });

        var result = await wrapper.RunAsync(query);

        Assert.IsType<ResultCursorWrapper>(result);
        Assert.IsType<Dictionary<string, object>>(capturedQuery.Parameters["entity"]);
    }

    private sealed class FakeNode : INode
    {
        public FakeNode(IReadOnlyDictionary<string, object> properties)
        {
            Properties = properties;
        }

        public object this[string key] => Properties[key];
        public IReadOnlyList<string> Labels { get; } = new[] { "Person" };
        public IReadOnlyDictionary<string, object> Properties { get; }
        public long Id => 1;
        public string ElementId => "node-1";

        public T Get<T>(string key)
        {
            return (T)Properties[key];
        }

        public bool TryGet<T>(string key, out T value)
        {
            if (Properties.TryGetValue(key, out var rawValue) && rawValue is T typedValue)
            {
                value = typedValue;
                return true;
            }

            value = default;
            return false;
        }

        public bool Equals(INode other)
        {
            return ReferenceEquals(this, other);
        }
    }

    private sealed class FakeRelationship : IRelationship
    {
        public FakeRelationship(IReadOnlyDictionary<string, object> properties)
        {
            Properties = properties;
        }

        public object this[string key] => Properties[key];
        public IReadOnlyDictionary<string, object> Properties { get; }
        public long Id => 2;
        public string ElementId => "relationship-2";
        public string Type => "ACTED_IN";
        public long StartNodeId => 1;
        public long EndNodeId => 3;
        public string StartNodeElementId => "node-1";
        public string EndNodeElementId => "node-3";

        public T Get<T>(string key)
        {
            return (T)Properties[key];
        }

        public bool TryGet<T>(string key, out T value)
        {
            if (Properties.TryGetValue(key, out var rawValue) && rawValue is T typedValue)
            {
                value = typedValue;
                return true;
            }

            value = default;
            return false;
        }

        public bool Equals(IRelationship other)
        {
            return ReferenceEquals(this, other);
        }
    }

    private sealed class FakePath : IPath
    {
        public FakePath(INode start, INode end, params IRelationship[] relationships)
        {
            Start = start;
            End = end;
            Nodes = new[] { start, end };
            Relationships = relationships;
        }

        public INode Start { get; }
        public INode End { get; }
        public IReadOnlyList<INode> Nodes { get; }
        public IReadOnlyList<IRelationship> Relationships { get; }

        public bool Equals(IPath other)
        {
            return ReferenceEquals(this, other);
        }
    }

    private sealed class FakeRecord : IRecord
    {
        private readonly IReadOnlyDictionary<string, object> values;

        public FakeRecord(IReadOnlyDictionary<string, object> values)
        {
            this.values = values;
            Keys = values.Keys.ToList();
        }

        public object this[int index] => values[Keys[index]];
        public object this[string key] => values[key];
        public IReadOnlyDictionary<string, object> Values => values;
        public IReadOnlyList<string> Keys { get; }
        IEnumerable<string> IReadOnlyDictionary<string, object>.Keys => Keys;
        IEnumerable<object> IReadOnlyDictionary<string, object>.Values => values.Values;
        public int Count => values.Count;

        public T Get<T>(string key)
        {
            return (T)values[key];
        }

        public bool TryGet<T>(string key, out T value)
        {
            if (values.TryGetValue(key, out var rawValue) && rawValue is T typedValue)
            {
                value = typedValue;
                return true;
            }

            value = default;
            return false;
        }

        public T GetCaseInsensitive<T>(string key)
        {
            return Get<T>(Keys.Single(existingKey =>
                string.Equals(existingKey, key, StringComparison.OrdinalIgnoreCase)));
        }

        public bool TryGetCaseInsensitive<T>(string key, out T value)
        {
            var existingKey = Keys.FirstOrDefault(candidate =>
                string.Equals(candidate, key, StringComparison.OrdinalIgnoreCase));
            if (existingKey != null)
                return TryGet(existingKey, out value);

            value = default;
            return false;
        }

        public bool ContainsKey(string key)
        {
            return values.ContainsKey(key);
        }

        public bool TryGetValue(string key, out object value)
        {
            return values.TryGetValue(key, out value);
        }

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            return values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    private sealed class FakeResultCursor : IResultCursor
    {
        private readonly IReadOnlyList<IRecord> records;
        private int index = -1;

        public FakeResultCursor(params IRecord[] records)
        {
            this.records = records;
        }

        public IRecord Current => index >= 0 && index < records.Count ? records[index] : null;
        public bool IsOpen => index < records.Count;

        public Task<string[]> KeysAsync()
        {
            return Task.FromResult(records.FirstOrDefault()?.Keys.ToArray() ?? Array.Empty<string>());
        }

        public Task<IResultSummary> ConsumeAsync()
        {
            index = records.Count;
            return Task.FromResult<IResultSummary>(null);
        }

        public Task<IRecord> PeekAsync()
        {
            var nextIndex = index + 1;
            return Task.FromResult(nextIndex < records.Count ? records[nextIndex] : null);
        }

        public Task<bool> FetchAsync()
        {
            index++;
            return Task.FromResult(index < records.Count);
        }

        public async IAsyncEnumerator<IRecord> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            foreach (var record in records)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Yield();
                yield return record;
            }
        }
    }
}
