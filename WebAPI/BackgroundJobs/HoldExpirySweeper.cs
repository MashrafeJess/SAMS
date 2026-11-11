using Business.Interface;

namespace WebAPI.BackgroundJobs
{
    /// <summary>
    /// Periodically returns lapsed holds to the seat pool.
    /// </summary>
    /// <remarks>
    /// This is housekeeping, not enforcement. A hold can always be expired but
    /// not yet swept, so <c>HoldService.CreateHold</c> keeps its own expiry
    /// check on the confirm path. Nothing about correctness depends on this
    /// service running -- if it stops, stale rows accumulate but no seat is
    /// double-issued.
    /// </remarks>
    public class HoldExpirySweeper(
        IServiceScopeFactory scopeFactory,
        ILogger<HoldExpirySweeper> logger) : BackgroundService
    {
        private static readonly TimeSpan Interval = TimeSpan.FromSeconds(30);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("Hold expiry sweeper started; interval {Interval}.", Interval);

            using var timer = new PeriodicTimer(Interval);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await SweepAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    // One bad tick must never kill the loop: an unhandled
                    // exception out of ExecuteAsync stops the service for the
                    // lifetime of the process, silently.
                    logger.LogError(ex, "Hold expiry sweep failed; will retry next tick.");
                }

                try
                {
                    if (!await timer.WaitForNextTickAsync(stoppingToken))
                    {
                        break;
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            logger.LogInformation("Hold expiry sweeper stopping.");
        }

        private async Task SweepAsync(CancellationToken ct)
        {
            // A BackgroundService is a singleton, but SamsDbContext (and so
            // IHoldService) is registered scoped. Taking IHoldService as a
            // constructor dependency would fail at startup with "Cannot consume
            // scoped service from singleton". Opening a scope per tick is the
            // supported pattern, and it keeps each DbContext short-lived rather
            // than letting one accumulate tracked entities for days.
            using var scope = scopeFactory.CreateScope();
            var holdService = scope.ServiceProvider.GetRequiredService<IHoldService>();

            var result = await holdService.ReleaseExpiredHolds(ct);

            if (result.Data is int released && released > 0)
            {
                logger.LogInformation("Released {Count} expired hold(s).", released);
            }
        }
    }
}
