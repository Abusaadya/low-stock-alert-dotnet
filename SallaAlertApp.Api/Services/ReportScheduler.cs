using SallaAlertApp.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace SallaAlertApp.Api.Services;

public class ReportScheduler : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ReportScheduler> _logger;

    public ReportScheduler(IServiceProvider serviceProvider, ILogger<ReportScheduler> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Report Scheduler started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.UtcNow;
                
                // Run checks every hour to see if we need to send reports
                // 1. Weekly Report: Fridays
                if (now.DayOfWeek == DayOfWeek.Friday)
                {
                    await ProcessWeeklyReports(stoppingToken);
                }

                // 2. Monthly Report: 1st of Month
                if (now.Day == 1)
                {
                    await ProcessMonthlyReports(stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ReportScheduler");
            }

            // Sleep for 1 hour to avoid spamming / heavy load
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }

    private async Task ProcessWeeklyReports(CancellationToken token)
    {
        using (var scope = _serviceProvider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var reportService = scope.ServiceProvider.GetRequiredService<ReportService>();

            var merchants = await context.Subscriptions
                .Where(s => s.Status == Models.SubscriptionStatus.Active || s.Status == Models.SubscriptionStatus.Trial)
                .ToListAsync(token);

            foreach (var sub in merchants)
            {
                // Check if already sent today
                if (sub.LastWeeklyReportSentAt.HasValue && sub.LastWeeklyReportSentAt.Value.Date == DateTime.UtcNow.Date)
                {
                    continue;
                }

                try 
                {
                    await reportService.SendWeeklyReport(sub.MerchantId);
                    _logger.LogInformation($"Weekly report sent to {sub.MerchantId}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to send weekly report to {sub.MerchantId}");
                }
            }
        }
    }

    private async Task ProcessMonthlyReports(CancellationToken token)
    {
        using (var scope = _serviceProvider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var reportService = scope.ServiceProvider.GetRequiredService<ReportService>();

            var merchants = await context.Subscriptions
                .Where(s => s.Status == Models.SubscriptionStatus.Active || s.Status == Models.SubscriptionStatus.Trial)
                .ToListAsync(token);

            foreach (var sub in merchants)
            {
                if (sub.LastMonthlyReportSentAt.HasValue && sub.LastMonthlyReportSentAt.Value.Date == DateTime.UtcNow.Date)
                {
                    continue;
                }

                try
                {
                    await reportService.SendMonthlyReport(sub.MerchantId);
                    _logger.LogInformation($"Monthly report sent to {sub.MerchantId}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to send monthly report to {sub.MerchantId}");
                }
            }
        }
    }
}
