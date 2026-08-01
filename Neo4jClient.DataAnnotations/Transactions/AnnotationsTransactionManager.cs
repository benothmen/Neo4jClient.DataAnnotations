using System;
using System.Threading.Tasks;
using Neo4j.Driver;
using Neo4jClient.DataAnnotations.Extensions.Driver;

namespace Neo4jClient.DataAnnotations.Transactions;

/// <summary>
/// Creates managed and explicit annotation-aware transactions. A fresh session is used for every operation.
/// </summary>
public sealed class AnnotationsTransactionManager
{
    private readonly IDriver driver;

    public AnnotationsTransactionManager(IDriver driver)
    {
        if (driver == null)
            throw new ArgumentNullException(nameof(driver));

        this.driver = driver is DriverWrapper ? driver : new DriverWrapper(driver);
    }

    public async Task<TResult> ExecuteReadAsync<TResult>(
        Func<IAnnotationsQueryRunner, Task<TResult>> work,
        Action<SessionConfigBuilder> configureSession = null,
        Action<TransactionConfigBuilder> configureTransaction = null)
    {
        if (work == null)
            throw new ArgumentNullException(nameof(work));

        await using var session = CreateSession(configureSession);
        return await session.ExecuteReadAsync(
            runner => work(new AnnotationsQueryRunner(runner)), configureTransaction);
    }

    public async Task ExecuteReadAsync(
        Func<IAnnotationsQueryRunner, Task> work,
        Action<SessionConfigBuilder> configureSession = null,
        Action<TransactionConfigBuilder> configureTransaction = null)
    {
        if (work == null)
            throw new ArgumentNullException(nameof(work));

        await using var session = CreateSession(configureSession);
        await session.ExecuteReadAsync(
            runner => work(new AnnotationsQueryRunner(runner)), configureTransaction);
    }

    public async Task<TResult> ExecuteWriteAsync<TResult>(
        Func<IAnnotationsQueryRunner, Task<TResult>> work,
        Action<SessionConfigBuilder> configureSession = null,
        Action<TransactionConfigBuilder> configureTransaction = null)
    {
        if (work == null)
            throw new ArgumentNullException(nameof(work));

        await using var session = CreateSession(configureSession);
        return await session.ExecuteWriteAsync(
            runner => work(new AnnotationsQueryRunner(runner)), configureTransaction);
    }

    public async Task ExecuteWriteAsync(
        Func<IAnnotationsQueryRunner, Task> work,
        Action<SessionConfigBuilder> configureSession = null,
        Action<TransactionConfigBuilder> configureTransaction = null)
    {
        if (work == null)
            throw new ArgumentNullException(nameof(work));

        await using var session = CreateSession(configureSession);
        await session.ExecuteWriteAsync(
            runner => work(new AnnotationsQueryRunner(runner)), configureTransaction);
    }

    public async Task<IAnnotationsTransaction> BeginTransactionAsync(
        Action<SessionConfigBuilder> configureSession = null,
        Action<TransactionConfigBuilder> configureTransaction = null)
    {
        var session = CreateSession(configureSession);

        try
        {
            var transaction = configureTransaction == null
                ? await session.BeginTransactionAsync()
                : await session.BeginTransactionAsync(configureTransaction);
            return new AnnotationsTransaction(session, transaction);
        }
        catch
        {
            await session.DisposeAsync();
            throw;
        }
    }

    private IAsyncSession CreateSession(Action<SessionConfigBuilder> configureSession)
    {
        return configureSession == null
            ? driver.AsyncSession()
            : driver.AsyncSession(configureSession);
    }
}
