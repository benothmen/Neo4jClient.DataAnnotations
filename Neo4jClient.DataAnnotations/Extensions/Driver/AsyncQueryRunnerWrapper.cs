using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Neo4j.Driver;
using Newtonsoft.Json.Linq;

namespace Neo4jClient.DataAnnotations.Extensions.Driver
{
    public class AsyncQueryRunnerWrapper : BaseWrapper<IAsyncQueryRunner>, IAsyncQueryRunner
    {
        public AsyncQueryRunnerWrapper(IAsyncQueryRunner queryRunner) : base(queryRunner)
        {
        }

        public async Task<IResultCursor> RunAsync(string query)
        {
            return SessionWrapper.GetResultCursor(await WrappedItem.RunAsync(query));
        }

        public Task<IResultCursor> RunAsync(string query, object parameters)
        {
            return RunAsync(new Query(query, parameters));
        }

        public Task<IResultCursor> RunAsync(string query, IDictionary<string, object> parameters)
        {
            return RunAsync(new Query(query, parameters));
        }

        public async Task<IResultCursor> RunAsync(Query query)
        {
            return SessionWrapper.GetResultCursor(await WrappedItem.RunAsync(NormalizeQuery(query)));
        }

        public ValueTask DisposeAsync()
        {
            return WrappedItem.DisposeAsync();
        }

        public void Dispose()
        {
            WrappedItem.Dispose();
        }

        internal static Query NormalizeQuery(Query query)
        {
            if (query?.Parameters?.Count <= 0)
                return query;

            foreach (var parameter in query.Parameters.ToArray())
                query.Parameters[parameter.Key] = NormalizeValue(parameter.Value);

            return query;
        }

        internal static Dictionary<string, object> NormalizeParameters(
            IDictionary<string, object> parameters)
        {
            return parameters?.ToDictionary(
                parameter => parameter.Key,
                parameter => NormalizeValue(parameter.Value));
        }

        internal static object NormalizeValue(object value)
        {
            if (value is JValue jValue)
                return jValue.Value;

            if (value is JObject jObject)
                return NormalizeParameters(
                    jObject.ToObject<Dictionary<string, object>>());

            if (value is JArray jArray)
                return jArray.Select(token => NormalizeValue(token)).ToList();

            if (value is IDictionary<string, object> dictionary)
                return NormalizeParameters(dictionary);

            if (value is IReadOnlyDictionary<string, object> readOnlyDictionary)
                return readOnlyDictionary.ToDictionary(
                    parameter => parameter.Key,
                    parameter => NormalizeValue(parameter.Value));

            if (value is IEnumerable sequence && value is not string && value is not byte[])
            {
                var normalized = new List<object>();
                foreach (var item in sequence)
                    normalized.Add(NormalizeValue(item));
                return normalized;
            }

            return value;
        }
    }
}
