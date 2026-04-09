using Microsoft.AspNetCore.Mvc;
using PdfMasterIndex.Service.Infrastructure.Persistence;

namespace PdfMasterIndex.Service.Presentation.v1;

[ApiController]
public class SettingsController(ISettingsRepository settingsRepository) : ControllerBase
{
    [HttpGet("/api/v1/settings/writable")]
    public ActionResult<bool> GetSettingsWritable()
    {
        return Ok(settingsRepository.SettingsWritable);
    }
}
