using CertificateSystem.BLL;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace CertificateSystem.Web.Services
{
    public class BatchPrintBackgroundService : BackgroundService
    {
        private readonly IBatchPrintQueue _queue;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<BatchPrintBackgroundService> _logger;

        public BatchPrintBackgroundService(
            IBatchPrintQueue queue,
            IServiceScopeFactory scopeFactory,
            ILogger<BatchPrintBackgroundService> logger)
        {
            _queue = queue;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                BatchPrintQueuedItem job;
                try
                {
                    job = await _queue.DequeueAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                try
                {
                    await ProcessJobAsync(job, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "批量打印任务处理异常. TaskId={TaskId}", job.TaskId);
                    _queue.MarkFailed(job.TaskId, ex.Message);
                }
            }
        }

        private async Task ProcessJobAsync(BatchPrintQueuedItem job, CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var certificateService = scope.ServiceProvider.GetRequiredService<ICertificateService>();
            var certificateGenerator = scope.ServiceProvider.GetRequiredService<ICertificateGenerator>();

            var listEntities = await certificateService.GetListByFilterAsync(job.Request.Filter, null);

            List<long> orderedIds;
            if (job.Request.Mode == BatchPrintMode.Selected)
            {
                var selectedIdSet = (job.Request.SelectedIds ?? new List<long>())
                    .Distinct()
                    .ToHashSet();

                orderedIds = listEntities
                    .Where(x => selectedIdSet.Contains(x.Id))
                    .Select(x => x.Id)
                    .ToList();
            }
            else
            {
                orderedIds = listEntities
                    .Select(x => x.Id)
                    .ToList();
            }

            if (orderedIds.Count == 0)
            {
                _queue.MarkFailed(job.TaskId, "没有可打印的数据。请检查筛选条件或选择项。");
                return;
            }

            _queue.MarkProcessing(job.TaskId, orderedIds.Count);

            var output = new PdfDocument();
            var processed = 0;

            foreach (var id in orderedIds)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var bytes = await certificateGenerator.GeneratePdfAsync(id, job.Request.CertificateTypeName);
                await using var ms = new MemoryStream(bytes);
                using var input = PdfReader.Open(ms, PdfDocumentOpenMode.Import);
                foreach (var page in input.Pages)
                {
                    output.AddPage(page);
                }

                processed++;
                _queue.ReportProgress(job.TaskId, processed, $"正在处理 {processed}/{orderedIds.Count}");
            }

            var tempDir = Path.Combine(Path.GetTempPath(), "CertificateSystem", "BatchPrint");
            Directory.CreateDirectory(tempDir);
            var filePath = Path.Combine(tempDir, $"{job.TaskId}.pdf");

            await using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                output.Save(fs, false);
            }
            output.Close();

            _queue.MarkCompleted(job.TaskId, filePath, $"已完成，共 {orderedIds.Count} 份证书。");
        }
    }
}
