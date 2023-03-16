using System.Diagnostics;
using Cronos;

namespace Telegram_AI_Bot.Infrastructure.BackGroundHosted;

public sealed class CronosPeriodicTimer : IDisposable
{
    private readonly CronExpression _cronExpression; // Also used as the locker
    private PeriodicTimer _activeTimer;
    private bool _disposed;
    private static readonly TimeSpan _minDelay = TimeSpan.FromMilliseconds(500);

    public CronosPeriodicTimer(string expression, CronFormat format)
    {
        _cronExpression = CronExpression.Parse(expression, format);
    }

    public async ValueTask<bool> WaitForNextTickAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        PeriodicTimer timer;
        lock (_cronExpression)
        {
            if (_disposed) return false;
            if (_activeTimer is not null)
                throw new InvalidOperationException("One consumer at a time.");
            DateTime utcNow = DateTime.UtcNow;
            DateTime? utcNext = _cronExpression.GetNextOccurrence(utcNow + _minDelay);
            if (utcNext is null)
                throw new InvalidOperationException("Unreachable date.");
            TimeSpan delay = utcNext.Value - utcNow;
            Debug.Assert(delay > _minDelay);
            timer = _activeTimer = new(delay);
        }
        try
        {
            // Dispose the timer after the first tick.
            using (timer)
                return await timer.WaitForNextTickAsync(cancellationToken)
                    .ConfigureAwait(false);
        }
        finally { Volatile.Write(ref _activeTimer, null); }
    }

    public void Dispose()
    {
        PeriodicTimer activeTimer;
        lock (_cronExpression)
        {
            if (_disposed) return;
            _disposed = true;
            activeTimer = _activeTimer;
        }
        activeTimer?.Dispose();
    }
}