using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Neo4j.Driver;

namespace Neo4jClient.DataAnnotations.Extensions.Driver
{
    public class SessionWrapper : BaseWrapper<IAsyncSession>, IAsyncSession
    {
        public SessionWrapper(IAsyncSession session) : base(session)
        {
        }

        public async Task<IResultCursor> RunAsync(string query)
        {
            return GetResultCursor(await WrappedItem.RunAsync(query));
        }

        public async Task<IResultCursor> RunAsync(string query, object parameters)
        {
            return await RunAsync(new Query(query, parameters));
        }

        public async Task<IResultCursor> RunAsync(string query, IDictionary<string, object> parameters)
        {
            return await RunAsync(new Query(query, parameters));
        }

        public async Task<IResultCursor> RunAsync(Query query)
        {
            return GetResultCursor(await WrappedItem.RunAsync(AsyncQueryRunnerWrapper.NormalizeQuery(query)));
        }

        public async Task<IAsyncTransaction> BeginTransactionAsync()
        {
            return GetAsyncTransaction(await WrappedItem.BeginTransactionAsync());
        }

        public async Task<IAsyncTransaction> BeginTransactionAsync(Action<TransactionConfigBuilder> action)
        {
            return GetAsyncTransaction(await WrappedItem.BeginTransactionAsync(action));
        }

        public Task<T> ReadTransactionAsync<T>(Func<IAsyncTransaction, Task<T>> work)
        {
            return WrappedItem.ReadTransactionAsync(tx => work(GetAsyncTransaction(tx)));
        }

        public Task ReadTransactionAsync(Func<IAsyncTransaction, Task> work)
        {
            return WrappedItem.ReadTransactionAsync(tx => work(GetAsyncTransaction(tx)));
        }

        public Task<T> ReadTransactionAsync<T>(Func<IAsyncTransaction, Task<T>> work,
            Action<TransactionConfigBuilder> action)
        {
            return WrappedItem.ReadTransactionAsync(tx => work(GetAsyncTransaction(tx)), action);
        }

        public Task ReadTransactionAsync(Func<IAsyncTransaction, Task> work, Action<TransactionConfigBuilder> action)
        {
            return WrappedItem.ReadTransactionAsync(tx => work(GetAsyncTransaction(tx)), action);
        }

        public Task<T> WriteTransactionAsync<T>(Func<IAsyncTransaction, Task<T>> work)
        {
            return WrappedItem.WriteTransactionAsync(tx => work(GetAsyncTransaction(tx)));
        }

        public Task WriteTransactionAsync(Func<IAsyncTransaction, Task> work)
        {
            return WrappedItem.WriteTransactionAsync(tx => work(GetAsyncTransaction(tx)));
        }

        public Task<T> WriteTransactionAsync<T>(Func<IAsyncTransaction, Task<T>> work,
            Action<TransactionConfigBuilder> action)
        {
            return WrappedItem.WriteTransactionAsync(tx => work(GetAsyncTransaction(tx)), action);
        }

        public Task WriteTransactionAsync(Func<IAsyncTransaction, Task> work, Action<TransactionConfigBuilder> action)
        {
            return WrappedItem.WriteTransactionAsync(tx => work(GetAsyncTransaction(tx)), action);
        }

        public Task<TResult> ExecuteReadAsync<TResult>(Func<IAsyncQueryRunner, Task<TResult>> work, Action<TransactionConfigBuilder> action = null)
        {
            return WrappedItem.ExecuteReadAsync(runner => work(GetAsyncQueryRunner(runner)), action);
        }

        public Task ExecuteReadAsync(Func<IAsyncQueryRunner, Task> work, Action<TransactionConfigBuilder> action = null)
        {
            return WrappedItem.ExecuteReadAsync(runner => work(GetAsyncQueryRunner(runner)), action);
        }

        public Task<TResult> ExecuteWriteAsync<TResult>(Func<IAsyncQueryRunner, Task<TResult>> work, Action<TransactionConfigBuilder> action = null)
        {
            return WrappedItem.ExecuteWriteAsync(runner => work(GetAsyncQueryRunner(runner)), action);
        }

        public Task ExecuteWriteAsync(Func<IAsyncQueryRunner, Task> work, Action<TransactionConfigBuilder> action = null)
        {
            return WrappedItem.ExecuteWriteAsync(runner => work(GetAsyncQueryRunner(runner)), action);
        }

        public Task CloseAsync()
        {
            return WrappedItem.CloseAsync();
        }

        public async Task<IResultCursor> RunAsync(string query, Action<TransactionConfigBuilder> action)
        {
            return GetResultCursor(await WrappedItem.RunAsync(query, action));
        }

        public async Task<IResultCursor> RunAsync(string query, IDictionary<string, object> parameters,
            Action<TransactionConfigBuilder> action)
        {
            return await RunAsync(new Query(query, parameters), action);
        }

        public async Task<IResultCursor> RunAsync(Query query, Action<TransactionConfigBuilder> action)
        {
            return GetResultCursor(
                await WrappedItem.RunAsync(AsyncQueryRunnerWrapper.NormalizeQuery(query), action));
        }

        public Bookmark LastBookmark => WrappedItem.LastBookmark;
        public Bookmarks LastBookmarks => WrappedItem.LastBookmarks;

        public SessionConfig SessionConfig => WrappedItem.SessionConfig;

        protected internal static IResultCursor GetResultCursor(IResultCursor resultCursor)
        {
            if (resultCursor != null && !(resultCursor is ResultCursorWrapper))
                return new ResultCursorWrapper(resultCursor);

            return resultCursor;
        }

        protected internal static IAsyncTransaction GetAsyncTransaction(IAsyncTransaction transaction)
        {
            if (transaction != null && !(transaction is AsyncTransactionWrapper))
                return new AsyncTransactionWrapper(transaction);

            return transaction;
        }

        protected internal static IAsyncQueryRunner GetAsyncQueryRunner(IAsyncQueryRunner queryRunner)
        {
            if (queryRunner is IAsyncTransaction transaction)
                return GetAsyncTransaction(transaction);

            if (queryRunner != null && queryRunner is not AsyncQueryRunnerWrapper)
                return new AsyncQueryRunnerWrapper(queryRunner);

            return queryRunner;
        }

        public ValueTask DisposeAsync()
        {
            return WrappedItem.DisposeAsync();
        }

        public void Dispose()
        {
            WrappedItem.Dispose();
        }
    }
}
