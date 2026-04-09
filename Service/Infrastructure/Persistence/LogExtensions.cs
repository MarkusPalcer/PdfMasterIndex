namespace PdfMasterIndex.Service.Infrastructure.Persistence;

public static partial class LogExtensions
{
    [LoggerMessage(LogLevel.Information, "Deleting ScanPath {ScanPathName} ({ScanPathId})...")]
    public static partial void DeletingScanPath(this ILogger logger, string? scanPathName, Guid scanPathId);

    [LoggerMessage(LogLevel.Information, "Deleting ScanPath record {ScanPathId}...")]
    public static partial void DeletingScanPathRecord(this ILogger logger, Guid scanPathId);

    [LoggerMessage(LogLevel.Information, "Successfully deleted ScanPath {ScanPathId}.")]
    public static partial void DeletedScanPathSuccess(this ILogger logger, Guid scanPathId);

    [LoggerMessage(LogLevel.Error, "Failed to delete ScanPath {ScanPathId}.")]
    public static partial void DeletedScanPathFailed(this ILogger logger, Guid scanPathId, Exception ex);

    [LoggerMessage(LogLevel.Information, "Clearing document {DocumentPath} ({DocumentId})...")]
    public static partial void ClearingDocument(this ILogger logger, string documentPath, Guid documentId);

    [LoggerMessage(LogLevel.Information, "Deleting occurrences for document {DocumentId}...")]
    public static partial void DeletingOccurrencesForDocument(this ILogger logger, Guid documentId);

    [LoggerMessage(LogLevel.Information, "Deleted {Count} occurrences for document {DocumentId}.")]
    public static partial void DeletedOccurrencesForDocumentCount(this ILogger logger, int count, Guid documentId);

    [LoggerMessage(LogLevel.Information, "Successfully cleared document {DocumentId}.")]
    public static partial void ClearedDocumentSuccess(this ILogger logger, Guid documentId);

    [LoggerMessage(LogLevel.Error, "Failed to clear document {DocumentId}.")]
    public static partial void ClearedDocumentFailed(this ILogger logger, Guid documentId, Exception ex);

    [LoggerMessage(LogLevel.Information, "Deleting orphaned words...")]
    public static partial void DeletingOrphanedWords(this ILogger logger);

    [LoggerMessage(LogLevel.Information, "Deleted {Count} orphaned words.")]
    public static partial void DeletedOrphanedWordsCount(this ILogger logger, int count);

    [LoggerMessage(LogLevel.Error, "Failed to delete orphaned words.")]
    public static partial void DeletedOrphanedWordsFailed(this ILogger logger, Exception ex);
}
