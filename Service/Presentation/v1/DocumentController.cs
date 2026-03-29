using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PdfMasterIndex.Service.Infrastructure.Persistence;
using PdfMasterIndex.Service.Infrastructure.Persistence.Models;

namespace PdfMasterIndex.Service.Presentation.v1;

[ApiController]
public class DocumentController(IRepository repository) : ControllerBase
{
    [HttpGet("/api/v1/documents")]
    public async Task<Document[]> Get() => await repository.Documents.ToArrayAsync();
    
    [HttpGet("/api/v1/documents/{id}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var result = await repository.Documents
                                     .Include(x => x.ScanPath)
                                     .SingleOrDefaultAsync(x => x.Id == id);

        if (result == null)
        {
            return NotFound();
        }

        var filePath = Path.Combine(result.ScanPath.InternalPath, result.FilePath);

        if (!System.IO.File.Exists(filePath))
        {
            return NotFound();
        }

        var stream = System.IO.File.OpenRead(filePath);
        return File(stream, "application/pdf", result.Name);
    }
}