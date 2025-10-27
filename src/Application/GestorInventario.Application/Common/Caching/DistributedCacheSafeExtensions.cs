namespace GestorInventario.Application.Common.Caching;

using System;
using System.Threading;
using GestorInventario.Application.Common.Interfaces.Caching;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

public static class DistributedCacheSafeExtensions
{
    private static readonly TimeSpan CacheOperationTimeout = TimeSpan.FromSeconds(1);

    public static async Task<string?> TryGetStringAsync(
        this IDistributedCache cache,
        string cacheKey,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (!DistributedCacheCircuitBreaker.ShouldAttemptOperation(cacheKey, logger, out _))
        {
            return null;
        }

        var (token, timeoutCts) = CreateTimeoutToken(cancellationToken);
        try
        {
            var result = await cache.GetStringAsync(cacheKey, token).ConfigureAwait(false);
            DistributedCacheCircuitBreaker.RecordSuccess();
            return result;
        }
        catch (OperationCanceledException ex) when (IsTimeout(ex, timeoutCts, cancellationToken))
        {
            LogCacheFailure(
                ex,
                logger,
                "Distributed cache read for {CacheKey} timed out after {Timeout}. Falling back to data source. Distributed cache operations will be bypassed for {Cooldown}.",
                new object[] { cacheKey, CacheOperationTimeout, DistributedCacheCircuitBreaker.Cooldown },
                "Distributed cache read for {CacheKey} timed out after {Timeout}. Falling back to data source.",
                new object[] { cacheKey, CacheOperationTimeout });

            return null;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogCacheFailure(
                ex,
                logger,
                "Failed to read distributed cache entry {CacheKey}. Falling back to data source. Distributed cache operations will be bypassed for {Cooldown}.",
                new object[] { cacheKey, DistributedCacheCircuitBreaker.Cooldown },
                "Failed to read distributed cache entry {CacheKey}. Falling back to data source.",
                new object[] { cacheKey });

            return null;
        }
        finally
        {
            timeoutCts.Dispose();
        }
    }

    public static async Task TrySetStringAsync(
        this IDistributedCache cache,
        string cacheKey,
        string value,
        DistributedCacheEntryOptions options,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (!DistributedCacheCircuitBreaker.ShouldAttemptOperation(cacheKey, logger, out _))
        {
            return;
        }

        var (token, timeoutCts) = CreateTimeoutToken(cancellationToken);
        try
        {
            await cache.SetStringAsync(cacheKey, value, options, token).ConfigureAwait(false);
            DistributedCacheCircuitBreaker.RecordSuccess();
        }
        catch (OperationCanceledException ex) when (IsTimeout(ex, timeoutCts, cancellationToken))
        {
            LogCacheFailure(
                ex,
                logger,
                "Distributed cache write for {CacheKey} timed out after {Timeout}. Continuing without caching. Distributed cache operations will be bypassed for {Cooldown}.",
                new object[] { cacheKey, CacheOperationTimeout, DistributedCacheCircuitBreaker.Cooldown },
                "Distributed cache write for {CacheKey} timed out after {Timeout}. Continuing without caching.",
                new object[] { cacheKey, CacheOperationTimeout });
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogCacheFailure(
                ex,
                logger,
                "Failed to write distributed cache entry {CacheKey}. Continuing without caching. Distributed cache operations will be bypassed for {Cooldown}.",
                new object[] { cacheKey, DistributedCacheCircuitBreaker.Cooldown },
                "Failed to write distributed cache entry {CacheKey}. Continuing without caching.",
                new object[] { cacheKey });
        }
        finally
        {
            timeoutCts.Dispose();
        }
    }

    public static async Task TryRegisterKeyAsync(
        this ICacheKeyRegistry cacheKeyRegistry,
        string region,
        string cacheKey,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var cacheContext = $"{cacheKey} (region: {region})";
        if (!DistributedCacheCircuitBreaker.ShouldAttemptOperation(cacheContext, logger, out _))
        {
            return;
        }

        var (token, timeoutCts) = CreateTimeoutToken(cancellationToken);
        try
        {
            await cacheKeyRegistry.RegisterKeyAsync(region, cacheKey, token).ConfigureAwait(false);
            DistributedCacheCircuitBreaker.RecordSuccess();
        }
        catch (OperationCanceledException ex) when (IsTimeout(ex, timeoutCts, cancellationToken))
        {
            LogCacheFailure(
                ex,
                logger,
                "Distributed cache key registration for {CacheKey} in region {CacheRegion} timed out after {Timeout}. Distributed cache operations will be bypassed for {Cooldown}.",
                new object[] { cacheKey, region, CacheOperationTimeout, DistributedCacheCircuitBreaker.Cooldown },
                "Distributed cache key registration for {CacheKey} in region {CacheRegion} timed out after {Timeout}.",
                new object[] { cacheKey, region, CacheOperationTimeout });
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogCacheFailure(
                ex,
                logger,
                "Failed to register distributed cache key {CacheKey} for region {CacheRegion}. Distributed cache operations will be bypassed for {Cooldown}.",
                new object[] { cacheKey, region, DistributedCacheCircuitBreaker.Cooldown },
                "Failed to register distributed cache key {CacheKey} for region {CacheRegion}.",
                new object[] { cacheKey, region });
        }
        finally
        {
            timeoutCts.Dispose();
        }
    }

    private static (CancellationToken Token, CancellationTokenSource TimeoutCts) CreateTimeoutToken(CancellationToken cancellationToken)
    {
        var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(CacheOperationTimeout);
        return (timeoutCts.Token, timeoutCts);
    }

    private static bool IsTimeout(OperationCanceledException exception, CancellationTokenSource timeoutCts, CancellationToken callerToken)
    {
        if (!timeoutCts.IsCancellationRequested || callerToken.IsCancellationRequested)
        {
            return false;
        }

        return exception.CancellationToken == timeoutCts.Token;
    }

    private static void LogCacheFailure(
        Exception exception,
        ILogger logger,
        string warningMessage,
        object[] warningArguments,
        string debugMessage,
        object[] debugArguments)
    {
        var shouldLogWarning = DistributedCacheCircuitBreaker.RecordFailure();
        if (shouldLogWarning)
        {
            logger.LogWarning(exception, warningMessage, warningArguments);
        }
        else
        {
            logger.LogDebug(exception, debugMessage, debugArguments);
        }
    }
}
