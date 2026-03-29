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
    public async Task<ActionResult<Document>> Get(Guid id)
    {
        var result = await repository.Documents.SingleOrDefaultAsync(x => x.Id == id);

        if (result == null)
        {
            return NotFound();
        }
        
        return result;
    }
}