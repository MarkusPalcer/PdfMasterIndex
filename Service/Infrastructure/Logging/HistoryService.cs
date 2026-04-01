using System.Collections.Concurrent;
using AutoInterfaceAttributes;
using PdfMasterIndex.Service.Attributes;

namespace PdfMasterIndex.Service.Infrastructure.Logging;

[AutoInterface]
[Lifetime(ServiceLifetime.Singleton)]
public class HistoryService : IHistoryService
{
    private readonly ConcurrentQueue<LogEntry> _logs = new();
    private const int MaxHistorySize = 100;

    public void AddLog(DateTime timestampUtc, string message, LogLevel severity, string category)
    {
        _logs.Enqueue(new LogEntry(timestampUtc, message, severity, category));

        while (_logs.Count > MaxHistorySize)
        {
            _logs.TryDequeue(out _);
        }
    }

    public IEnumerable<LogEntry> GetRecentLogs() => _logs.ToArray();
}
