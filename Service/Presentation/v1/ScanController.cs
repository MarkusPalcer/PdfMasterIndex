using Microsoft.AspNetCore.Mvc;
using PdfMasterIndex.Service.Application;

namespace PdfMasterIndex.Service.Presentation.v1;


[ApiController]
public class ScanController(IScanner scanner) : ControllerBase
{
    [HttpPost("/api/v1/scan")]
    public IActionResult Post()
    {
        try
        {
            scanner.Start();
            return Accepted();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }

    [HttpGet("/api/v1/scan")]
    public ActionResult<ScanStatus> Get()
    {
        return scanner.Status;
    }

    [HttpDelete("/api/v1/scan")]
    public async Task<IActionResult> Delete()
    {
        try
        {
            await scanner.Cancel();
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }
}