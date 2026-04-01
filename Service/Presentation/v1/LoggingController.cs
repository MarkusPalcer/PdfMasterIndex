using Microsoft.AspNetCore.Mvc;
using PdfMasterIndex.Service.Infrastructure.Logging;

namespace PdfMasterIndex.Service.Presentation.v1;

[ApiController]
public class LoggingController(IHistoryService historyService) : ControllerBase
{
    [HttpGet("/api/v1/log")]
    public IEnumerable<LogEntry> Get() => historyService.GetRecentLogs();
}