namespace GestorInventario.Application.Common.Caching;

using System.Threading;
using Microsoft.Extensions.Logging;

internal static class DistributedCacheCircuitBreaker
{
    private static readonly TimeSpan CooldownPeriod = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan BypassLogInterval = TimeSpan.FromSeconds(30);

    private static long openUntilTimestamp;
    private static long lastBypassLogTimestamp;

    public static TimeSpan Cooldown => CooldownPeriod;

    public static bool ShouldAttemptOperation(string cacheContext, ILogger logger, out TimeSpan? remainingCooldown)
    {
        var nowMilliseconds = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var openUntil = Interlocked.Read(ref openUntilTimestamp);
        if (openUntil <= nowMilliseconds)
        {
            remainingCooldown = null;
            return true;
        }

        remainingCooldown = TimeSpan.FromMilliseconds(openUntil - nowMilliseconds);
        if (logger.IsEnabled(LogLevel.Debug))
        {
            LogBypass(cacheContext, logger, remainingCooldown.Value, nowMilliseconds);
        }

        return false;
    }

    public static bool RecordFailure()
    {
        var nowMilliseconds = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var desiredOpenUntil = DateTimeOffset.UtcNow.Add(CooldownPeriod).ToUnixTimeMilliseconds();
        var current = Interlocked.Read(ref openUntilTimestamp);

        if (current <= nowMilliseconds)
        {
            var original = Interlocked.CompareExchange(ref openUntilTimestamp, desiredOpenUntil, current);
            if (original == current)
            {
                return true;
            }
        }

        return false;
    }

    public static void RecordSuccess()
    {
        Interlocked.Exchange(ref openUntilTimestamp, 0);
    }

    private static void LogBypass(string cacheContext, ILogger logger, TimeSpan remaining, long nowMilliseconds)
    {
        var lastLog = Interlocked.Read(ref lastBypassLogTimestamp);
        if (nowMilliseconds - lastLog >= (long)BypassLogInterval.TotalMilliseconds)
        {
            if (Interlocked.CompareExchange(ref lastBypassLogTimestamp, nowMilliseconds, lastLog) == lastLog)
            {
                logger.LogDebug(
                    "Skipping distributed cache operation for {CacheContext} because the distributed cache is unavailable. Remaining cooldown {Cooldown}.",
                    cacheContext,
                    remaining);
            }
        }
    }
}
