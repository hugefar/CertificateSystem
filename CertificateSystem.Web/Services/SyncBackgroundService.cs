using CertificateSystem.BLL;

namespace CertificateSystem.Web.Services
{
    public class SyncBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SyncBackgroundService> _logger;

        public SyncBackgroundService(IServiceScopeFactory scopeFactory, ILogger<SyncBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var delay = GetDelayUntilNextRun();
                await Task.Delay(delay, stoppingToken);

                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var certificateSyncService = scope.ServiceProvider.GetRequiredService<IStudentSyncService>();
                    var paperSyncService = scope.ServiceProvider.GetRequiredService<IPaperSyncService>();

                    try
                    {
                        var certificateResult = await certificateSyncService.SyncAsync(stoppingToken);
                        if (!certificateResult.Success)
                        {
                            _logger.LogWarning("定时同步学生证书数据失败：{Message}", certificateResult.Message);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "定时同步学生证书数据失败。");
                    }

                    try
                    {
                        var paperResult = await paperSyncService.SyncPapersAsync(stoppingToken);
                        if (!paperResult.Success)
                        {
                            _logger.LogWarning("定时同步学生论文数据失败：{Message}", paperResult.Message);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "定时同步学生论文数据失败。");
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "定时同步学生证书数据失败。");
                }
            }
        }

        private static TimeSpan GetDelayUntilNextRun()
        {
            var now = DateTime.Now;
            var next = now.Date.AddDays(1);
            return next - now;
        }
    }
}
