using System.Collections.Concurrent;
using AutoInterfaceAttributes;
using Microsoft.AspNetCore.SignalR;
using PdfMasterIndex.Service.Attributes;
using PdfMasterIndex.Service.Presentation.v1;

namespace PdfMasterIndex.Service.Infrastructure.Logging;

[AutoInterface]
[Lifetime(ServiceLifetime.Singleton)]
public class HistoryService(IServiceProvider serviceProvider) : IHistoryService
{
    private readonly ConcurrentQueue<LogEntry> _logs = new();
    private const int MaxHistorySize = 100;

    public void AddLog(DateTime timestampUtc, string message, LogLevel severity, string category)
    {
        var logEntry = new LogEntry(timestampUtc, message, severity, category);
        _logs.Enqueue(logEntry);
        
        while (_logs.Count > MaxHistorySize)
        {
            _logs.TryDequeue(out _);
        }

        var hubContext = serviceProvider.GetService<IHubContext<LogHub>>();
        hubContext?.Clients.All.SendAsync("Log", logEntry);
    }

    public IEnumerable<LogEntry> GetRecentLogs() => _logs.ToArray();
}
