using System.Collections.Concurrent;
using System.Threading.Channels;

namespace CertificateSystem.Web.Services
{
    public class MemoryBatchPrintQueue : IBatchPrintQueue
    {
        private readonly Channel<BatchPrintQueuedItem> _channel = Channel.CreateUnbounded<BatchPrintQueuedItem>();
        private readonly ConcurrentDictionary<string, BatchPrintTaskSnapshot> _tasks = new();

        public string Enqueue(BatchPrintTaskRequest request)
        {
            var taskId = Guid.NewGuid().ToString("N");
            var snapshot = new BatchPrintTaskSnapshot
            {
                TaskId = taskId,
                CertificateTypeKey = request.CertificateTypeKey,
                CertificateTypeName = request.CertificateTypeName,
                Status = BatchPrintTaskStatus.Pending,
                CreatedAt = DateTime.Now,
                Message = "任务已创建，等待处理",
                OperatorName = request.OperatorName
            };

            _tasks[taskId] = snapshot;
            _channel.Writer.TryWrite(new BatchPrintQueuedItem
            {
                TaskId = taskId,
                Request = request
            });
            return taskId;
        }

        public async ValueTask<BatchPrintQueuedItem> DequeueAsync(CancellationToken cancellationToken)
        {
            return await _channel.Reader.ReadAsync(cancellationToken);
        }

        public BatchPrintTaskSnapshot? GetSnapshot(string taskId)
        {
            if (_tasks.TryGetValue(taskId, out var s))
                return new BatchPrintTaskSnapshot
                {
                    TaskId = s.TaskId,
                    CertificateTypeKey = s.CertificateTypeKey,
                    CertificateTypeName = s.CertificateTypeName,
                    Status = s.Status,
                    TotalCount = s.TotalCount,
                    ProcessedCount = s.ProcessedCount,
                    Message = s.Message,
                    ResultFilePath = s.ResultFilePath,
                    CreatedAt = s.CreatedAt,
                    CompletedAt = s.CompletedAt,
                    OperatorName = s.OperatorName,
                    PrintedAt = s.PrintedAt,
                    Certificates = s.Certificates
                        .Select(x => new BatchPrintCertificateItem
                        {
                            CertificateId = x.CertificateId,
                            Institute = x.Institute,
                            StudentId = x.StudentId,
                            Name = x.Name
                        })
                        .ToList()
                };

            return null;
        }

        public void SetCertificates(string taskId, List<BatchPrintCertificateItem> certificates)
        {
            if (_tasks.TryGetValue(taskId, out var s))
            {
                s.Certificates = certificates ?? new List<BatchPrintCertificateItem>();
            }
        }

        public void MarkProcessing(string taskId, int totalCount)
        {
            if (_tasks.TryGetValue(taskId, out var s))
            {
                s.Status = BatchPrintTaskStatus.Processing;
                s.TotalCount = totalCount;
                s.ProcessedCount = 0;
                s.Message = "正在生成并合并 PDF...";
            }
        }

        public void ReportProgress(string taskId, int processedCount, string? message = null)
        {
            if (_tasks.TryGetValue(taskId, out var s))
            {
                s.ProcessedCount = processedCount;
                if (!string.IsNullOrWhiteSpace(message))
                    s.Message = message;
            }
        }

        public void MarkCompleted(string taskId, string filePath, string? message = null)
        {
            if (_tasks.TryGetValue(taskId, out var s))
            {
                s.Status = BatchPrintTaskStatus.Completed;
                s.ResultFilePath = filePath;
                s.ProcessedCount = s.TotalCount;
                s.CompletedAt = DateTime.Now;
                s.Message = message ?? "处理完成";
            }
        }

        public void MarkFailed(string taskId, string errorMessage)
        {
            if (_tasks.TryGetValue(taskId, out var s))
            {
                s.Status = BatchPrintTaskStatus.Failed;
                s.CompletedAt = DateTime.Now;
                s.Message = errorMessage;
            }
        }

        public void MarkPrinted(string taskId)
        {
            if (_tasks.TryGetValue(taskId, out var s))
            {
                s.PrintedAt = DateTime.Now;
            }
        }
    }
}
