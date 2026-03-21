using buronet_service.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace buronet_service.Services
{
    /// <summary>
    /// Background service that periodically cleans up expired pending user registrations.
    /// Runs every hour to delete pending users whose confirmation tokens have expired.
    /// </summary>
    public class PendingUserCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<PendingUserCleanupService> _logger;
        private static readonly TimeSpan CleanupInterval = TimeSpan.FromHours(1);

        public PendingUserCleanupService(IServiceProvider serviceProvider, ILogger<PendingUserCleanupService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("PendingUserCleanupService started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CleanupExpiredPendingUsersAsync();
                    await Task.Delay(CleanupInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("PendingUserCleanupService is stopping.");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred in PendingUserCleanupService.");
                    // Continue running even if an error occurs
                    await Task.Delay(CleanupInterval, stoppingToken);
                }
            }
        }

        private async Task CleanupExpiredPendingUsersAsync()
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var now = DateTime.UtcNow;

                // Find all pending users whose tokens have expired
                var expiredPendingUsers = await context.PendingUsers
                    .Where(pu => pu.TokenExpiresAt < now && !pu.ConfirmedAt.HasValue)
                    .ToListAsync();

                if (expiredPendingUsers.Count > 0)
                {
                    context.PendingUsers.RemoveRange(expiredPendingUsers);
                    await context.SaveChangesAsync();

                    _logger.LogInformation(
                        $"Cleaned up {expiredPendingUsers.Count} expired pending user(s) at {now:O}");
                }
            }
        }
    }
}
