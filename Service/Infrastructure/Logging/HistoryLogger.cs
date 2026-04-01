namespace PdfMasterIndex.Service.Infrastructure.Logging;

public class HistoryLogger(string categoryName, IHistoryService historyService) : ILogger
{
    public IDisposable BeginScope<TState>(TState state) where TState : notnull => null!;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var message = formatter(state, exception);
        historyService.AddLog(DateTime.UtcNow, message, logLevel, categoryName);
    }
}
