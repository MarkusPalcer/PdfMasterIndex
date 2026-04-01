using System.Collections.Concurrent;

namespace PdfMasterIndex.Service.Infrastructure.Logging;

public class HistoryLoggerProvider(IHistoryService historyService) : ILoggerProvider
{
    private readonly ConcurrentDictionary<string, HistoryLogger> _loggers = new(StringComparer.OrdinalIgnoreCase);

    public ILogger CreateLogger(string categoryName)
    {
        return _loggers.GetOrAdd(categoryName, name => new HistoryLogger(name, historyService));
    }

    public void Dispose()
    {
        _loggers.Clear();
    }
}
