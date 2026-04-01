namespace PdfMasterIndex.Service.Application.Scanning;

public static partial class LogExtensions
{
    [LoggerMessage(LogLevel.Trace, "Scanning for files started")]
    public static partial void ScanningStarted(this ILogger logger);
    
    [LoggerMessage(LogLevel.Trace, "Scanning for files in {Path}...")]
    public static partial void ScanningStarted(this ILogger logger, string path);
    
    [LoggerMessage(LogLevel.Trace, "Checking file {Path}...")]
    public static partial void ScanningFile(this ILogger logger, string path);
    
    [LoggerMessage(LogLevel.Trace, "New file -> Queued for processing")]
    public static partial void NewFile(this ILogger logger);
    
    [LoggerMessage(LogLevel.Trace, "Changed file -> Queued for processing")]
    public static partial void ChangedFile(this ILogger logger);
    
    [LoggerMessage(LogLevel.Trace, "Removed file {File} -> Removed from database")]
    public static partial void RemovedFile(this ILogger logger, string file);
    
    [LoggerMessage(LogLevel.Error, "Scanning {Path} failed")]
    public static partial void ScanningFailed(this ILogger logger, string path, Exception exception);
    
    [LoggerMessage(LogLevel.Trace, "Scanning for files finished")]
    public static partial void ScanningFinished(this ILogger logger);
    
    [LoggerMessage(LogLevel.Trace, "Importing files started")]
    public static partial void ImportStarted(this ILogger logger);
    
    [LoggerMessage(LogLevel.Trace, "Importing file {Path}...")]
    public static partial void ImportProgress(this ILogger logger, string path);
    
    [LoggerMessage(LogLevel.Trace, "Importing files finished")]
    public static partial void ImportFinished(this ILogger logger);
    
    [LoggerMessage(LogLevel.Error, "Importing file {Path} failed")]
    public static partial void ImportFailed(this ILogger logger, string path, Exception exception);
    
    [LoggerMessage(LogLevel.Error, "Uncaught exception during scan")]
    public static partial void UncaughtException(this ILogger logger, Exception exception);
    
    [LoggerMessage(LogLevel.Information, "Scan cancelled")]
    public static partial void Cancelled(this ILogger logger);
    
    [LoggerMessage(LogLevel.Error, "The word {word} on page {page} is too long; skipping")]
    public static partial void WordTooLong(this ILogger logger, string word, int page); 
}