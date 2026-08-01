using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Neo4j.Driver;

namespace Neo4jClient.DataAnnotations.Extensions.Driver;

public class RecordWrapper : BaseWrapper<IRecord>, IRecord
{
    private IReadOnlyDictionary<string, object> values;

    public RecordWrapper(IRecord record) : base(record)
    {
    }

    public T Get<T>(string key)
    {
        return GetWrappedValue(WrappedItem.Get<T>(key));
    }

    public bool TryGet<T>(string key, out T value)
    {
        if (!WrappedItem.TryGet(key, out T rawValue))
        {
            value = default;
            return false;
        }

        value = GetWrappedValue(rawValue);
        return true;
    }

    public T GetCaseInsensitive<T>(string key)
    {
        return GetWrappedValue(WrappedItem.GetCaseInsensitive<T>(key));
    }

    public bool TryGetCaseInsensitive<T>(string key, out T value)
    {
        if (!WrappedItem.TryGetCaseInsensitive(key, out T rawValue))
        {
            value = default;
            return false;
        }

        value = GetWrappedValue(rawValue);
        return true;
    }

    public object this[int index] => GetValue(WrappedItem[index]);

    public bool ContainsKey(string key)
    {
        return Values.ContainsKey(key);
    }

    public bool TryGetValue(string key, out object value)
    {
        if (!WrappedItem.TryGetValue(key, out var rawValue))
        {
            value = null;
            return false;
        }

        value = GetValue(rawValue);
        return true;
    }

    public object this[string key] => GetValue(WrappedItem[key]);
    IEnumerable<string> IReadOnlyDictionary<string, object>.Keys => Keys;

    IEnumerable<object> IReadOnlyDictionary<string, object>.Values => Values.Values;

    public IReadOnlyDictionary<string, object> Values
    {
        get
        {
            if (values == null)
                values = WrappedItem.Values.ToDictionary(v => v.Key, v => GetValue(v.Value));

            return values;
        }
    }

    public IReadOnlyList<string> Keys => WrappedItem.Keys;

    protected static object GetValue(object value)
    {
        if (value is IPath path && value is not PathWrapper)
            return new PathWrapper(path);
        if (value is INode node && value is not NodeWrapper)
            return new NodeWrapper(node);
        if (value is IRelationship relationship && value is not RelationshipWrapper)
            return new RelationshipWrapper(relationship);

        if (value is IEnumerable<INode> nodes)
            return nodes.Select(nodeValue => nodeValue is NodeWrapper
                ? nodeValue
                : (INode)new NodeWrapper(nodeValue)).ToList();

        if (value is IEnumerable<IRelationship> relationships)
            return relationships.Select(relationshipValue => relationshipValue is RelationshipWrapper
                ? relationshipValue
                : (IRelationship)new RelationshipWrapper(relationshipValue)).ToList();

        if (value is IReadOnlyDictionary<string, object> dictionary)
            return dictionary.ToDictionary(pair => pair.Key, pair => GetValue(pair.Value));

        if (value is IEnumerable<object> sequence && value is not string)
        {
            var originalValues = sequence.ToList();
            var wrappedValues = originalValues.Select(GetValue).ToList();

            if (wrappedValues.Where((wrapped, index) => !ReferenceEquals(wrapped, originalValues[index])).Any())
                return wrappedValues;
        }

        return value;
    }

    private static T GetWrappedValue<T>(T value)
    {
        var wrappedValue = GetValue(value);
        return wrappedValue is T typedValue ? typedValue : value;
    }

    public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
    {
        return Values.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public int Count => Values.Count;
}
