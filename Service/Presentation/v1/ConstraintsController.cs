using Microsoft.AspNetCore.Mvc;
using PdfMasterIndex.Service.Infrastructure.Persistence;

namespace PdfMasterIndex.Service.Presentation.v1;

[ApiController]
public class ConstraintsController(IPersistenceMetadata metadata) : ControllerBase
{
    [HttpGet("/api/v1/constraints")]
    public ActionResult<string[]> GetModelNames()
    {
        return metadata.GetModelNames();
    }

    [HttpGet("/api/v1/constraints/{modelName}")]
    public ActionResult<ModelConstraints> GetModelConstraints(string modelName)
    {
        try
        {
            return metadata.GetModelConstraints(modelName);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}