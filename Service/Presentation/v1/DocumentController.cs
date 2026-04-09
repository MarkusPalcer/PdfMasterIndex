using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PdfMasterIndex.Service.Domain.Index;
using PdfMasterIndex.Service.Infrastructure.Persistence;

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

        var filePath = Path.Combine(result.ScanPath.Path, result.FilePath);

        if (!System.IO.File.Exists(filePath))
        {
            return NotFound();
        }

        var stream = System.IO.File.OpenRead(filePath);
        return File(stream, "application/pdf", result.Name);
    }

    [HttpGet("/api/v1/documents/{id}/tags")]
    public async Task<ActionResult<string[]>> GetTags(Guid id)
    {
        var result = await repository.Documents
                                     .Include(x => x.Tags)
                                     .SingleOrDefaultAsync(x => x.Id == id);

        if (result == null)
        {
            return NotFound();
        }

        return Ok(result.Tags.Select(t => t.Value).ToArray());
    }

    [HttpPost("/api/v1/documents/{id}/tags/{tag}")]
    public async Task<IActionResult> AddTag(Guid id, string tag)
    {
        var existing = await repository.Documents
                                     .Include(x => x.Tags)
                                     .SingleOrDefaultAsync(x => x.Id == id);

        if (existing == null) return NotFound();
        if (existing.Tags.Any(t => t.Value == tag)) return NoContent();

        var tagEntity = (await repository.ProcessTags([tag])).First();
        existing.Tags.Add(tagEntity);
        await repository.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("/api/v1/documents/{id}/tags/{tag}")]
    public async Task<IActionResult> RemoveTag(Guid id, string tag)
    {
        var existing = await repository.Documents
                                       .Include(x => x.Tags)
                                       .SingleOrDefaultAsync(x => x.Id == id);
        if (existing == null) return NotFound();
        
        var tagToRemove = existing.Tags.FirstOrDefault(t => t.Value == tag);
        if (tagToRemove == null) return NoContent();

        existing.Tags.Remove(tagToRemove);
        await repository.SaveChangesAsync();
        
        return NoContent();
    }
}