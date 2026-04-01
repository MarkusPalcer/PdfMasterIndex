namespace PdfMasterIndex.Service.Infrastructure.Logging;

public record LogEntry(DateTime TimestampUtc, string Message, LogLevel Severity, string Category);
