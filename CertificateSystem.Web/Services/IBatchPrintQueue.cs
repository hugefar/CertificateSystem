namespace CertificateSystem.Web.Services
{
    public interface IBatchPrintQueue
    {
        string Enqueue(BatchPrintTaskRequest request);
        ValueTask<BatchPrintQueuedItem> DequeueAsync(CancellationToken cancellationToken);
        BatchPrintTaskSnapshot? GetSnapshot(string taskId);
        void MarkProcessing(string taskId, int totalCount);
        void ReportProgress(string taskId, int processedCount, string? message = null);
        void MarkCompleted(string taskId, string filePath, string? message = null);
        void MarkFailed(string taskId, string errorMessage);
        void MarkPrinted(string taskId);
    }
}
